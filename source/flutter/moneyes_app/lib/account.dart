//https://medium.com/theotherdev-s/getting-to-know-flutter-sqflite-deb664588114

class Account {
  final String ownerName;
  final String name;
  final String number;
  final String? bic;
  final String iban;
  final String bankCode;

  Account(
      {required this.ownerName,
      required this.name,
      required this.number,
      this.bic,
      required this.iban,
      required this.bankCode});

  factory Account.fromJson(Map<String, dynamic> json) {
    return Account(
      ownerName: json['ownerName'],
      number: json['number'],
      bankCode: json['bankCode'],
      iban: json['iban'],
      name: json['type'],
      bic: json['bic'],
    );
  }
}