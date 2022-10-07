using Microsoft.Extensions.Logging;
using Moneyes.Core.Filters;
using Moneyes.Data;
using LiteDB;

namespace Moneyes.UI.Blazor
{
    class MauiDatabaseInitializer : IMauiInitializeService
    {
        public async void Initialize(IServiceProvider services)
        {
            var dbProvider = services.GetRequiredService<IDatabaseProvider<ILiteDatabase>>();
            var dbLogger = services.GetRequiredService<ILogger<ILiteDatabase>>();

            // Open / create database

            if (!dbProvider.IsDatabaseCreated)
            {
                if (!await dbProvider.TryCreateDatabase())
                {
                    Environment.Exit(-1);
                }
            }
            else
            {
                if (!await dbProvider.TryOpenDatabase())
                {
                    Environment.Exit(-1);
                }
            }

            ApplyMigrations(dbProvider.Database, dbLogger);
        }

        private static void ApplyMigrations(ILiteDatabase database, ILogger<ILiteDatabase> logger)
        {
            if (database.UserVersion == 0)
            {
                logger.LogInformation("Database version is 0. Migrating to version 1...");


                logger.LogInformation("Migrating Categories...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();
                var categoryIdGuidMap = new Dictionary<int, Guid>();

                foreach (var document in categoryDocuments)
                {
                    categories.Delete(document["_id"]);
                    var guid = Guid.NewGuid();
                    categoryIdGuidMap.Add(document["_id"].AsInt32, guid);
                    document["_id"] = new BsonValue(guid);
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in categoryDocuments)
                {
                    if (!document.ContainsKey("Parent"))
                    {
                        continue;
                    }

                    var oldId = document["Parent"].AsDocument["$id"];
                    document["Parent"]["$id"] = categoryIdGuidMap[oldId];
                }

                categories.Insert(categoryDocuments);

                logger.LogInformation("Migrating Transactions...");

                var transactions = database.GetCollection("Transaction");
                var transactionDocuments = transactions.FindAll().ToList();


                foreach (var document in transactionDocuments)
                {
                    transactions.Delete(document["_id"]);
                    document["UID"] = new BsonValue(document["_id"].AsString);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in transactionDocuments)
                {
                    if (!document.ContainsKey("Categories"))
                    {
                        continue;
                    }

                    foreach (var refDoc in document["Categories"].AsArray)
                    {
                        refDoc["$id"] = categoryIdGuidMap[refDoc["$id"].AsInt32];
                    }
                }

                transactions.Insert(transactionDocuments);

                logger.LogInformation("Migrating Accounts...");

                var accounts = database.GetCollection("AccountDetails");
                var accountDocuments = accounts.FindAll().ToList();
                var accountIbanGuidMap = new Dictionary<string, Guid>();

                foreach (var document in accountDocuments)
                {
                    accounts.Delete(document["_id"]);
                    document["IBAN"] = new BsonValue(document["_id"].AsString);
                    var guid = Guid.NewGuid();
                    accountIbanGuidMap[document["_id"].AsString] = guid;
                    document["_id"] = new BsonValue(guid);
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                    accounts.Insert(document);
                }

                logger.LogInformation("Migrating Balances...");

                var balances = database.GetCollection("Balance");
                var balanceDocuments = balances.FindAll().ToList();

                foreach (var document in balanceDocuments)
                {
                    balances.Delete(document["_id"]);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                }

                foreach (var document in balanceDocuments.ToList())
                {
                    if (!document.ContainsKey("Account"))
                    {
                        continue;
                    }

                    var oldId = document["Account"].AsDocument["$id"];

                    if (!accountIbanGuidMap.ContainsKey(oldId))
                    {
                        balanceDocuments.Remove(document);
                        continue;
                    }

                    document["Account"]["$id"] = accountIbanGuidMap[oldId];
                }

                balances.Insert(balanceDocuments);

                logger.LogInformation("Migrating online banking connections...");

                var bankingDetails = database.GetCollection("OnlineBankingDetails");
                var bankingDetailsDocuments = bankingDetails.FindAll().ToList();

                foreach (var document in bankingDetailsDocuments)
                {
                    bankingDetails.Delete(document["_id"]);
                    document["_id"] = new BsonValue(Guid.NewGuid());
                    document["CreatedAt"] = DateTime.UtcNow;
                    document["UpdatedAt"] = DateTime.UtcNow;
                    document["IsDeleted"] = false;
                    bankingDetails.Insert(document);
                }

                database.RenameCollection("OnlineBankingDetails", "BankDetails");

                logger.LogInformation("Migration successful");

                database.UserVersion = 1;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 1");
            }
            if (database.UserVersion == 1)
            {
                logger.LogInformation("Database version is 1. Migrating to version 2...");


                logger.LogInformation("Migrating Accounts...");

                var banks = database.GetCollection("BankDetails");
                var bankDocuments = banks.FindAll().ToList();

                var accounts = database.GetCollection("AccountDetails");
                var accountDocuments = accounts.FindAll().ToList();

                foreach (var document in accountDocuments)
                {
                    accounts.Delete(document["_id"]);

                    string bankCode = document["BankCode"];
                    var bank = bankDocuments.FirstOrDefault(d => d["BankCode"].AsInt32.ToString().Equals(bankCode));

                    if (bank != null)
                    {
                        var bankId = bank["_id"];
                        document["Bank"] = new BsonDocument { ["$id"] = bankId, ["$ref"] = "BankDetails" };
                    }

                    document.Remove("BankCode");

                    document["UpdatedAt"] = DateTime.UtcNow;

                    accounts.Insert(document);

                }

                database.RenameCollection("AccountDetails", "Accounts");


                logger.LogInformation("Migrating Categories...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();

                foreach (var document in categoryDocuments)
                {
                    categories.Delete(document["_id"]);

                    var filter = BsonMapper.Global.Deserialize<TransactionFilter>(document["Filter"]);

                    if (filter == null)
                    {
                        continue;
                    }

                    var newFilterDocument = BsonMapper.Global.ToDocument(filter.ToDto());

                    document["Filter"] = newFilterDocument;
                }

                categories.Insert(categoryDocuments);


                logger.LogInformation("Migrating Balances...");

                var balances = database.GetCollection("Balance");
                var balanceDocuments = balances.FindAll().ToList();

                foreach (var document in balanceDocuments)
                {
                    balances.Delete(document["_id"]);
                    document["Account"]["$ref"] = new BsonValue("Accounts");
                }

                balances.Insert(balanceDocuments);


                logger.LogInformation("Migration successful");

                database.UserVersion = 2;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 2");

            }

            if (database.UserVersion == 2)
            {
                logger.LogInformation("Database version is 2. Migrating to version 3...");

                var categories = database.GetCollection("Category");
                var categoryDocuments = categories.FindAll().ToList();
                var categoryIdMap = new Dictionary<Guid, BsonDocument>();

                foreach (var document in categoryDocuments)
                {
                    categoryIdMap.Add(document["_id"], document);
                }


                logger.LogInformation("Migrating Transactions...");

                var transactions = database.GetCollection("Transaction");
                var transactionDocuments = transactions.FindAll().ToList();


                foreach (var document in transactionDocuments)
                {
                    transactions.Delete(document["_id"]);

                    var categoryIds = document["Categories"].AsArray
                        .Select(x => x["$id"])
                        .ToList();

                    if (!categoryIds.Any())
                    {
                        document["Category"] = null;
                        document.Remove("Categories");

                        continue;
                    }

                    var firstWithParent = categoryIds
                    .FirstOrDefault(cid => !categoryIdMap[cid]["Parent"].IsNull);

                    if (firstWithParent is null)
                    {
                        var firstCategoryId = categoryIds.FirstOrDefault();
                        document["Category"] = new BsonDocument { ["$id"] = firstCategoryId, ["$ref"] = "Category" };
                    }
                    else
                    {
                        document["Category"] = new BsonDocument { ["$id"] = firstWithParent, ["$ref"] = "Category" };
                    }

                    document.Remove("Categories");
                    document["UpdatedAt"] = DateTime.UtcNow;
                }

                transactions.Insert(transactionDocuments);


                logger.LogInformation("Migration successful");

                database.UserVersion = 3;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 3");
            }

            if (database.UserVersion == 3)
            {
                logger.LogInformation("Database version is 3. Migrating to version 4...");

                logger.LogInformation("Migrating bank details...");

                var banks = database.GetCollection("BankDetails");
                var bankDocuments = banks.FindAll().ToList();

                foreach (var document in bankDocuments)
                {
                    var bankCode = document["BankCode"].AsInt32;

                    var fintsInstitute = Moneyes.LiveData.BankInstitutes.GetInstitute(bankCode);

                    document["Name"] = fintsInstitute.Name;
                    document["Server"] = fintsInstitute.FinTs_Url;
                    document["HbciVersion"] = fintsInstitute.Version.Contains("3.0") ? 300 : null;
                    document["UpdateAt"] = DateTime.UtcNow;
                }

                banks.Update(bankDocuments);

                logger.LogInformation("Migration successful");

                database.UserVersion = 4;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 4");
            }

            if (database.UserVersion == 4)
            {
                logger.LogInformation("Database version is 4. Migrating to version 5...");

                logger.LogInformation("Dropping old indeces...");

                database.GetCollection("Category").DropIndex("Name");
                database.GetCollection("Transaction").DropIndex("UID");

                logger.LogInformation("Migration successful");

                database.UserVersion = 5;

                database.Checkpoint();

                logger.LogInformation("Migrated to version 5");
            }
        }
    }
}