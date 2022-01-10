class TransactionDto {
  final String? uid;
  final String? altName;
  final String? name;
  final String? partnerIBAN;
  final String? bic;
  final String iban;
  final String? purpose;
  final double amount;
  final int index;
  final String? currency;
  final String bookingType;
  final DateTime bookingDate;
  final DateTime? valueDate;

  TransactionDto(
      {this.uid,
      this.altName,
      this.name,
      this.partnerIBAN,
      this.bic,
      required this.iban,
      this.purpose,
      required this.amount,
      required this.index,
      this.currency,
      required this.bookingType,
      required this.bookingDate,
      this.valueDate});

  factory TransactionDto.fromJson(Map<String, dynamic> json) {
    return TransactionDto(
        uid: json['uid'],
        altName: json['altName'],
        name: json['name'],
        partnerIBAN: json['partnerIBAN'],
        iban: json['iban'],
        purpose: json['purpose'],
        bic: json['bic'],
        amount: json['amount'],
        index: json['index'],
        currency: json['currency'],
        bookingType: json['bookingType'],
        bookingDate: DateTime.parse(json['bookingDate']),
        valueDate: DateTime.parse(json['valueDate']));
  }

  Map<String, dynamic> toJson() {
    return {
      'uid': uid,
      'name': name,
      'altName': altName,
      'partnerIban': partnerIBAN,
      'iban': iban,
      'purpose': purpose,
      'bic': bic,
      'amount': amount,
      'index': index,
      'currency': currency,
      'bookingType': bookingType,
      'bookingDate': bookingDate.toIso8601String(),
      'valueDate': valueDate?.toIso8601String()
    };
  }

  TransactionDto copy(
      {String? uid,
      String? altName,
      String? name,
      String? partnerIBAN,
      String? bic,
      String? iban,
      String? purpose,
      double? amount,
      int? index,
      String? currency,
      String? bookingType,
      DateTime? bookingDate,
      DateTime? valueDate}) {
    return TransactionDto(
        uid: uid ?? this.uid,
        altName: this.altName,
        name: name ?? this.name,
        partnerIBAN: partnerIBAN ?? this.partnerIBAN,
        bic: bic ?? this.bic,
        iban: iban ?? this.iban,
        purpose: purpose ?? this.purpose,
        amount: amount ?? this.amount,
        index: index ?? this.index,
        currency: currency ?? this.currency,
        bookingType: bookingType ?? this.bookingType,
        bookingDate: bookingDate ?? this.bookingDate,
        valueDate: valueDate ?? this.valueDate);
  }
}
