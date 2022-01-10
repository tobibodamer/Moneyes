import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'account.dart';
import 'transaction.dart';

class BankService {
  final _client = http.Client();
  String cookie = "";

  Future<void> login() async {
    debugPrint("login triggered");

    var createConnectionData = {
      'bankCode': dotenv.env['BANK_CODE'],
      'userId': dotenv.env['USER_ID'],
      'pin': dotenv.env['PIN'],
      'testConnection': true
    };

    var response = await _client.post(Uri.https(baseUri, "/login"),
        headers: {
          'Content-Type': 'application/json',
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Credentials": 'true',
          "Access-Control-Allow-Headers":
              "Origin,Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token,locale",
          "Access-Control-Allow-Methods": "POST"
        },
        body: jsonEncode(createConnectionData));

    debugPrint('status code: ${response.statusCode}');

    if (response.statusCode == 200) {
      debugPrint("login success");
      updateCookie(response);
    }
  }

  final String baseUri = "localhost:44385";

  Map<String, String> _getHeaders() {
    return {
      'Accept': 'application/json',
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Headers": "Origin,Content-Type,X-Amz-Date,X-Api-Key,X-Amz-Security-Token,locale",
      "Access-Control-Allow-Methods": "GET",
      "cookie": cookie
    };
  }

  Future<List<Account>> getAccounts() async {
    debugPrint("get accounts");

    var response = await _client.get(Uri.https(baseUri, "/accounts"), headers: _getHeaders());

    debugPrint('status code: ${response.statusCode}');

    if (response.statusCode == 200) {
      debugPrint(response.body);

      Iterable jsonMap = jsonDecode(response.body);
      var accounts = List<Account>.from(jsonMap.map((j) => Account.fromJson(j)));
      debugPrint("accounts success");
      return accounts;
    }

    return List<Account>.empty();
  }

  Future<List<TransactionDto>> getTransactions(Account account, DateTime startDate, DateTime endDate) async {
    debugPrint("get transactions");

    var queryParameters = <String, String>{
      "startDate": startDate.toString(),
      "endDate": endDate.toString(),
      "accountNumber": account.number,
      "iban": account.iban
    };

    Uri url = Uri.https(baseUri, "/transactions", queryParameters);

    var response = await _client.get(url, headers: _getHeaders());

    debugPrint('status code: ${response.statusCode}');

    if (response.statusCode == 200) {
      debugPrint(response.body);

      Map<String, dynamic> jsonMap = jsonDecode(response.body);

      Iterable transactionsMap = jsonMap["transactions"];
      //Iterable balancesMap = jsonMap["balances"];

      var transactions = List<TransactionDto>.from(transactionsMap.map((j) => TransactionDto.fromJson(j)));

      return transactions;
    }

    return List<TransactionDto>.empty();
  }

  void updateCookie(http.Response response) {
    String? rawCookie = response.headers['set-cookie'];
    if (rawCookie != null) {
      int index = rawCookie.indexOf(';');
      cookie = (index == -1) ? rawCookie : rawCookie.substring(0, index);
    }
  }
}
