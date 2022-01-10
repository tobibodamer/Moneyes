import 'package:moor_flutter/moor_flutter.dart';

@DataClassName("Transaction")
class Transactions extends Table {
  TextColumn get uid => text().nullable()();
  IntColumn get idx => integer()();
  TextColumn get name => text().nullable()();
  TextColumn get altName => text().nullable()();
  TextColumn get currency => text().nullable()();
  RealColumn get amount => real()();
  TextColumn get iban => text().nullable()();
  TextColumn get partnerIban => text().nullable()();
  TextColumn get bic => text().nullable()();
  DateTimeColumn get bookingDate => dateTime()();
  DateTimeColumn get valueDate => dateTime().nullable()();
  TextColumn get purpose => text().nullable()();
  TextColumn get bookingType => text().nullable()();

  @override
  Set<Column> get primaryKey => {uid};

  // @override
  // List<String> get customConstraints => [
  //       'UNIQUE (transaction_index, amount, booking_date, iban, partner_iban, bic) ON CONFLICT REPLACE'
  //     ];
}
