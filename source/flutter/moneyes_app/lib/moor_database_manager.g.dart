// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'moor_database_manager.dart';

// **************************************************************************
// MoorGenerator
// **************************************************************************

// ignore_for_file: unnecessary_brace_in_string_interps, unnecessary_this
class Transaction extends DataClass implements Insertable<Transaction> {
  final String? uid;
  final int idx;
  final String? name;
  final String? altName;
  final String? currency;
  final double amount;
  final String? iban;
  final String? partnerIban;
  final String? bic;
  final DateTime bookingDate;
  final DateTime? valueDate;
  final String? purpose;
  final String? bookingType;
  Transaction(
      {this.uid,
      required this.idx,
      this.name,
      this.altName,
      this.currency,
      required this.amount,
      this.iban,
      this.partnerIban,
      this.bic,
      required this.bookingDate,
      this.valueDate,
      this.purpose,
      this.bookingType});
  factory Transaction.fromData(Map<String, dynamic> data, GeneratedDatabase db,
      {String? prefix}) {
    final effectivePrefix = prefix ?? '';
    return Transaction(
      uid: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}uid']),
      idx: const IntType()
          .mapFromDatabaseResponse(data['${effectivePrefix}idx'])!,
      name: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}name']),
      altName: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}alt_name']),
      currency: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}currency']),
      amount: const RealType()
          .mapFromDatabaseResponse(data['${effectivePrefix}amount'])!,
      iban: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}iban']),
      partnerIban: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}partner_iban']),
      bic: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}bic']),
      bookingDate: const DateTimeType()
          .mapFromDatabaseResponse(data['${effectivePrefix}booking_date'])!,
      valueDate: const DateTimeType()
          .mapFromDatabaseResponse(data['${effectivePrefix}value_date']),
      purpose: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}purpose']),
      bookingType: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}booking_type']),
    );
  }
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (!nullToAbsent || uid != null) {
      map['uid'] = Variable<String?>(uid);
    }
    map['idx'] = Variable<int>(idx);
    if (!nullToAbsent || name != null) {
      map['name'] = Variable<String?>(name);
    }
    if (!nullToAbsent || altName != null) {
      map['alt_name'] = Variable<String?>(altName);
    }
    if (!nullToAbsent || currency != null) {
      map['currency'] = Variable<String?>(currency);
    }
    map['amount'] = Variable<double>(amount);
    if (!nullToAbsent || iban != null) {
      map['iban'] = Variable<String?>(iban);
    }
    if (!nullToAbsent || partnerIban != null) {
      map['partner_iban'] = Variable<String?>(partnerIban);
    }
    if (!nullToAbsent || bic != null) {
      map['bic'] = Variable<String?>(bic);
    }
    map['booking_date'] = Variable<DateTime>(bookingDate);
    if (!nullToAbsent || valueDate != null) {
      map['value_date'] = Variable<DateTime?>(valueDate);
    }
    if (!nullToAbsent || purpose != null) {
      map['purpose'] = Variable<String?>(purpose);
    }
    if (!nullToAbsent || bookingType != null) {
      map['booking_type'] = Variable<String?>(bookingType);
    }
    return map;
  }

  TransactionsCompanion toCompanion(bool nullToAbsent) {
    return TransactionsCompanion(
      uid: uid == null && nullToAbsent ? const Value.absent() : Value(uid),
      idx: Value(idx),
      name: name == null && nullToAbsent ? const Value.absent() : Value(name),
      altName: altName == null && nullToAbsent
          ? const Value.absent()
          : Value(altName),
      currency: currency == null && nullToAbsent
          ? const Value.absent()
          : Value(currency),
      amount: Value(amount),
      iban: iban == null && nullToAbsent ? const Value.absent() : Value(iban),
      partnerIban: partnerIban == null && nullToAbsent
          ? const Value.absent()
          : Value(partnerIban),
      bic: bic == null && nullToAbsent ? const Value.absent() : Value(bic),
      bookingDate: Value(bookingDate),
      valueDate: valueDate == null && nullToAbsent
          ? const Value.absent()
          : Value(valueDate),
      purpose: purpose == null && nullToAbsent
          ? const Value.absent()
          : Value(purpose),
      bookingType: bookingType == null && nullToAbsent
          ? const Value.absent()
          : Value(bookingType),
    );
  }

  factory Transaction.fromJson(Map<String, dynamic> json,
      {ValueSerializer? serializer}) {
    serializer ??= moorRuntimeOptions.defaultSerializer;
    return Transaction(
      uid: serializer.fromJson<String?>(json['uid']),
      idx: serializer.fromJson<int>(json['idx']),
      name: serializer.fromJson<String?>(json['name']),
      altName: serializer.fromJson<String?>(json['altName']),
      currency: serializer.fromJson<String?>(json['currency']),
      amount: serializer.fromJson<double>(json['amount']),
      iban: serializer.fromJson<String?>(json['iban']),
      partnerIban: serializer.fromJson<String?>(json['partnerIban']),
      bic: serializer.fromJson<String?>(json['bic']),
      bookingDate: serializer.fromJson<DateTime>(json['bookingDate']),
      valueDate: serializer.fromJson<DateTime?>(json['valueDate']),
      purpose: serializer.fromJson<String?>(json['purpose']),
      bookingType: serializer.fromJson<String?>(json['bookingType']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= moorRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'uid': serializer.toJson<String?>(uid),
      'idx': serializer.toJson<int>(idx),
      'name': serializer.toJson<String?>(name),
      'altName': serializer.toJson<String?>(altName),
      'currency': serializer.toJson<String?>(currency),
      'amount': serializer.toJson<double>(amount),
      'iban': serializer.toJson<String?>(iban),
      'partnerIban': serializer.toJson<String?>(partnerIban),
      'bic': serializer.toJson<String?>(bic),
      'bookingDate': serializer.toJson<DateTime>(bookingDate),
      'valueDate': serializer.toJson<DateTime?>(valueDate),
      'purpose': serializer.toJson<String?>(purpose),
      'bookingType': serializer.toJson<String?>(bookingType),
    };
  }

  Transaction copyWith(
          {String? uid,
          int? idx,
          String? name,
          String? altName,
          String? currency,
          double? amount,
          String? iban,
          String? partnerIban,
          String? bic,
          DateTime? bookingDate,
          DateTime? valueDate,
          String? purpose,
          String? bookingType}) =>
      Transaction(
        uid: uid ?? this.uid,
        idx: idx ?? this.idx,
        name: name ?? this.name,
        altName: altName ?? this.altName,
        currency: currency ?? this.currency,
        amount: amount ?? this.amount,
        iban: iban ?? this.iban,
        partnerIban: partnerIban ?? this.partnerIban,
        bic: bic ?? this.bic,
        bookingDate: bookingDate ?? this.bookingDate,
        valueDate: valueDate ?? this.valueDate,
        purpose: purpose ?? this.purpose,
        bookingType: bookingType ?? this.bookingType,
      );
  @override
  String toString() {
    return (StringBuffer('Transaction(')
          ..write('uid: $uid, ')
          ..write('idx: $idx, ')
          ..write('name: $name, ')
          ..write('altName: $altName, ')
          ..write('currency: $currency, ')
          ..write('amount: $amount, ')
          ..write('iban: $iban, ')
          ..write('partnerIban: $partnerIban, ')
          ..write('bic: $bic, ')
          ..write('bookingDate: $bookingDate, ')
          ..write('valueDate: $valueDate, ')
          ..write('purpose: $purpose, ')
          ..write('bookingType: $bookingType')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(uid, idx, name, altName, currency, amount,
      iban, partnerIban, bic, bookingDate, valueDate, purpose, bookingType);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is Transaction &&
          other.uid == this.uid &&
          other.idx == this.idx &&
          other.name == this.name &&
          other.altName == this.altName &&
          other.currency == this.currency &&
          other.amount == this.amount &&
          other.iban == this.iban &&
          other.partnerIban == this.partnerIban &&
          other.bic == this.bic &&
          other.bookingDate == this.bookingDate &&
          other.valueDate == this.valueDate &&
          other.purpose == this.purpose &&
          other.bookingType == this.bookingType);
}

class TransactionsCompanion extends UpdateCompanion<Transaction> {
  final Value<String?> uid;
  final Value<int> idx;
  final Value<String?> name;
  final Value<String?> altName;
  final Value<String?> currency;
  final Value<double> amount;
  final Value<String?> iban;
  final Value<String?> partnerIban;
  final Value<String?> bic;
  final Value<DateTime> bookingDate;
  final Value<DateTime?> valueDate;
  final Value<String?> purpose;
  final Value<String?> bookingType;
  const TransactionsCompanion({
    this.uid = const Value.absent(),
    this.idx = const Value.absent(),
    this.name = const Value.absent(),
    this.altName = const Value.absent(),
    this.currency = const Value.absent(),
    this.amount = const Value.absent(),
    this.iban = const Value.absent(),
    this.partnerIban = const Value.absent(),
    this.bic = const Value.absent(),
    this.bookingDate = const Value.absent(),
    this.valueDate = const Value.absent(),
    this.purpose = const Value.absent(),
    this.bookingType = const Value.absent(),
  });
  TransactionsCompanion.insert({
    this.uid = const Value.absent(),
    required int idx,
    this.name = const Value.absent(),
    this.altName = const Value.absent(),
    this.currency = const Value.absent(),
    required double amount,
    this.iban = const Value.absent(),
    this.partnerIban = const Value.absent(),
    this.bic = const Value.absent(),
    required DateTime bookingDate,
    this.valueDate = const Value.absent(),
    this.purpose = const Value.absent(),
    this.bookingType = const Value.absent(),
  })  : idx = Value(idx),
        amount = Value(amount),
        bookingDate = Value(bookingDate);
  static Insertable<Transaction> custom({
    Expression<String?>? uid,
    Expression<int>? idx,
    Expression<String?>? name,
    Expression<String?>? altName,
    Expression<String?>? currency,
    Expression<double>? amount,
    Expression<String?>? iban,
    Expression<String?>? partnerIban,
    Expression<String?>? bic,
    Expression<DateTime>? bookingDate,
    Expression<DateTime?>? valueDate,
    Expression<String?>? purpose,
    Expression<String?>? bookingType,
  }) {
    return RawValuesInsertable({
      if (uid != null) 'uid': uid,
      if (idx != null) 'idx': idx,
      if (name != null) 'name': name,
      if (altName != null) 'alt_name': altName,
      if (currency != null) 'currency': currency,
      if (amount != null) 'amount': amount,
      if (iban != null) 'iban': iban,
      if (partnerIban != null) 'partner_iban': partnerIban,
      if (bic != null) 'bic': bic,
      if (bookingDate != null) 'booking_date': bookingDate,
      if (valueDate != null) 'value_date': valueDate,
      if (purpose != null) 'purpose': purpose,
      if (bookingType != null) 'booking_type': bookingType,
    });
  }

  TransactionsCompanion copyWith(
      {Value<String?>? uid,
      Value<int>? idx,
      Value<String?>? name,
      Value<String?>? altName,
      Value<String?>? currency,
      Value<double>? amount,
      Value<String?>? iban,
      Value<String?>? partnerIban,
      Value<String?>? bic,
      Value<DateTime>? bookingDate,
      Value<DateTime?>? valueDate,
      Value<String?>? purpose,
      Value<String?>? bookingType}) {
    return TransactionsCompanion(
      uid: uid ?? this.uid,
      idx: idx ?? this.idx,
      name: name ?? this.name,
      altName: altName ?? this.altName,
      currency: currency ?? this.currency,
      amount: amount ?? this.amount,
      iban: iban ?? this.iban,
      partnerIban: partnerIban ?? this.partnerIban,
      bic: bic ?? this.bic,
      bookingDate: bookingDate ?? this.bookingDate,
      valueDate: valueDate ?? this.valueDate,
      purpose: purpose ?? this.purpose,
      bookingType: bookingType ?? this.bookingType,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (uid.present) {
      map['uid'] = Variable<String?>(uid.value);
    }
    if (idx.present) {
      map['idx'] = Variable<int>(idx.value);
    }
    if (name.present) {
      map['name'] = Variable<String?>(name.value);
    }
    if (altName.present) {
      map['alt_name'] = Variable<String?>(altName.value);
    }
    if (currency.present) {
      map['currency'] = Variable<String?>(currency.value);
    }
    if (amount.present) {
      map['amount'] = Variable<double>(amount.value);
    }
    if (iban.present) {
      map['iban'] = Variable<String?>(iban.value);
    }
    if (partnerIban.present) {
      map['partner_iban'] = Variable<String?>(partnerIban.value);
    }
    if (bic.present) {
      map['bic'] = Variable<String?>(bic.value);
    }
    if (bookingDate.present) {
      map['booking_date'] = Variable<DateTime>(bookingDate.value);
    }
    if (valueDate.present) {
      map['value_date'] = Variable<DateTime?>(valueDate.value);
    }
    if (purpose.present) {
      map['purpose'] = Variable<String?>(purpose.value);
    }
    if (bookingType.present) {
      map['booking_type'] = Variable<String?>(bookingType.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('TransactionsCompanion(')
          ..write('uid: $uid, ')
          ..write('idx: $idx, ')
          ..write('name: $name, ')
          ..write('altName: $altName, ')
          ..write('currency: $currency, ')
          ..write('amount: $amount, ')
          ..write('iban: $iban, ')
          ..write('partnerIban: $partnerIban, ')
          ..write('bic: $bic, ')
          ..write('bookingDate: $bookingDate, ')
          ..write('valueDate: $valueDate, ')
          ..write('purpose: $purpose, ')
          ..write('bookingType: $bookingType')
          ..write(')'))
        .toString();
  }
}

class $TransactionsTable extends Transactions
    with TableInfo<$TransactionsTable, Transaction> {
  final GeneratedDatabase _db;
  final String? _alias;
  $TransactionsTable(this._db, [this._alias]);
  final VerificationMeta _uidMeta = const VerificationMeta('uid');
  @override
  late final GeneratedColumn<String?> uid = GeneratedColumn<String?>(
      'uid', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _idxMeta = const VerificationMeta('idx');
  @override
  late final GeneratedColumn<int?> idx = GeneratedColumn<int?>(
      'idx', aliasedName, false,
      type: const IntType(), requiredDuringInsert: true);
  final VerificationMeta _nameMeta = const VerificationMeta('name');
  @override
  late final GeneratedColumn<String?> name = GeneratedColumn<String?>(
      'name', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _altNameMeta = const VerificationMeta('altName');
  @override
  late final GeneratedColumn<String?> altName = GeneratedColumn<String?>(
      'alt_name', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _currencyMeta = const VerificationMeta('currency');
  @override
  late final GeneratedColumn<String?> currency = GeneratedColumn<String?>(
      'currency', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _amountMeta = const VerificationMeta('amount');
  @override
  late final GeneratedColumn<double?> amount = GeneratedColumn<double?>(
      'amount', aliasedName, false,
      type: const RealType(), requiredDuringInsert: true);
  final VerificationMeta _ibanMeta = const VerificationMeta('iban');
  @override
  late final GeneratedColumn<String?> iban = GeneratedColumn<String?>(
      'iban', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _partnerIbanMeta =
      const VerificationMeta('partnerIban');
  @override
  late final GeneratedColumn<String?> partnerIban = GeneratedColumn<String?>(
      'partner_iban', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _bicMeta = const VerificationMeta('bic');
  @override
  late final GeneratedColumn<String?> bic = GeneratedColumn<String?>(
      'bic', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _bookingDateMeta =
      const VerificationMeta('bookingDate');
  @override
  late final GeneratedColumn<DateTime?> bookingDate =
      GeneratedColumn<DateTime?>('booking_date', aliasedName, false,
          type: const IntType(), requiredDuringInsert: true);
  final VerificationMeta _valueDateMeta = const VerificationMeta('valueDate');
  @override
  late final GeneratedColumn<DateTime?> valueDate = GeneratedColumn<DateTime?>(
      'value_date', aliasedName, true,
      type: const IntType(), requiredDuringInsert: false);
  final VerificationMeta _purposeMeta = const VerificationMeta('purpose');
  @override
  late final GeneratedColumn<String?> purpose = GeneratedColumn<String?>(
      'purpose', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _bookingTypeMeta =
      const VerificationMeta('bookingType');
  @override
  late final GeneratedColumn<String?> bookingType = GeneratedColumn<String?>(
      'booking_type', aliasedName, true,
      type: const StringType(), requiredDuringInsert: false);
  @override
  List<GeneratedColumn> get $columns => [
        uid,
        idx,
        name,
        altName,
        currency,
        amount,
        iban,
        partnerIban,
        bic,
        bookingDate,
        valueDate,
        purpose,
        bookingType
      ];
  @override
  String get aliasedName => _alias ?? 'transactions';
  @override
  String get actualTableName => 'transactions';
  @override
  VerificationContext validateIntegrity(Insertable<Transaction> instance,
      {bool isInserting = false}) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('uid')) {
      context.handle(
          _uidMeta, uid.isAcceptableOrUnknown(data['uid']!, _uidMeta));
    }
    if (data.containsKey('idx')) {
      context.handle(
          _idxMeta, idx.isAcceptableOrUnknown(data['idx']!, _idxMeta));
    } else if (isInserting) {
      context.missing(_idxMeta);
    }
    if (data.containsKey('name')) {
      context.handle(
          _nameMeta, name.isAcceptableOrUnknown(data['name']!, _nameMeta));
    }
    if (data.containsKey('alt_name')) {
      context.handle(_altNameMeta,
          altName.isAcceptableOrUnknown(data['alt_name']!, _altNameMeta));
    }
    if (data.containsKey('currency')) {
      context.handle(_currencyMeta,
          currency.isAcceptableOrUnknown(data['currency']!, _currencyMeta));
    }
    if (data.containsKey('amount')) {
      context.handle(_amountMeta,
          amount.isAcceptableOrUnknown(data['amount']!, _amountMeta));
    } else if (isInserting) {
      context.missing(_amountMeta);
    }
    if (data.containsKey('iban')) {
      context.handle(
          _ibanMeta, iban.isAcceptableOrUnknown(data['iban']!, _ibanMeta));
    }
    if (data.containsKey('partner_iban')) {
      context.handle(
          _partnerIbanMeta,
          partnerIban.isAcceptableOrUnknown(
              data['partner_iban']!, _partnerIbanMeta));
    }
    if (data.containsKey('bic')) {
      context.handle(
          _bicMeta, bic.isAcceptableOrUnknown(data['bic']!, _bicMeta));
    }
    if (data.containsKey('booking_date')) {
      context.handle(
          _bookingDateMeta,
          bookingDate.isAcceptableOrUnknown(
              data['booking_date']!, _bookingDateMeta));
    } else if (isInserting) {
      context.missing(_bookingDateMeta);
    }
    if (data.containsKey('value_date')) {
      context.handle(_valueDateMeta,
          valueDate.isAcceptableOrUnknown(data['value_date']!, _valueDateMeta));
    }
    if (data.containsKey('purpose')) {
      context.handle(_purposeMeta,
          purpose.isAcceptableOrUnknown(data['purpose']!, _purposeMeta));
    }
    if (data.containsKey('booking_type')) {
      context.handle(
          _bookingTypeMeta,
          bookingType.isAcceptableOrUnknown(
              data['booking_type']!, _bookingTypeMeta));
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {uid};
  @override
  Transaction map(Map<String, dynamic> data, {String? tablePrefix}) {
    return Transaction.fromData(data, _db,
        prefix: tablePrefix != null ? '$tablePrefix.' : null);
  }

  @override
  $TransactionsTable createAlias(String alias) {
    return $TransactionsTable(_db, alias);
  }
}

abstract class _$AppDatabase extends GeneratedDatabase {
  _$AppDatabase(QueryExecutor e) : super(SqlTypeSystem.defaultInstance, e);
  late final $TransactionsTable transactions = $TransactionsTable(this);
  @override
  Iterable<TableInfo> get allTables => allSchemaEntities.whereType<TableInfo>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities => [transactions];
}
