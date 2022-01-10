import 'dart:io';

import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart' as paths;
import 'package:moor/moor.dart';
import 'package:moor/ffi.dart';

import 'transaction.dart';
import 'transactions_table.dart';

part 'moor_database_manager.g.dart';

@UseMoor(tables: [Transactions])
class AppDatabase extends _$AppDatabase {
  AppDatabase(QueryExecutor e) : super(e);

  @override
  int get schemaVersion => 1;

  Future<List<Transaction>> getAllTransactions() => select(transactions).get();

  Future setAllTransactions(Iterable<TransactionDto> t) => batch((batch) {
        var companions = t.map((t) => TransactionsCompanion.insert(
            uid: Value(t.uid),
            idx: t.index,
            amount: t.amount,
            bookingDate: t.bookingDate,
            altName: Value(t.altName),
            bic: Value(t.bic),
            name: Value(t.name),
            currency: Value(t.currency),
            iban: Value(t.iban),
            partnerIban: Value(t.partnerIBAN),
            valueDate: Value(t.valueDate),
            purpose: Value(t.purpose),
            bookingType: Value(t.bookingType)));

        batch.insertAll(transactions, companions, mode: InsertMode.insertOrReplace);
      });
}

AppDatabase openConnection({bool logStatements = false}) {
  final executor = LazyDatabase(() async {
    final dataDir = await paths.getApplicationDocumentsDirectory();
    final dbFile = File(p.join(dataDir.path, 'db.sqlite'));
    return VmDatabase(dbFile, logStatements: logStatements);
  });
  return AppDatabase(executor);
}
