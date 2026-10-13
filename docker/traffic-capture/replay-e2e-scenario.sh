#!/bin/sh
# Реплей демо-сценария TripSplit отдельным средством отправки запросов (curl) -
# не тем же xUnit E2E-тестом (ТИОПО ЛР2, З5). Шаги совпадают по смыслу с
# src/TripSplit.E2E.Tests/TripLifecycleE2ETests.cs: регистрация двух пользователей,
# создание поездки, приглашение, добавление траты, просмотр расчета, завершение поездки.
set -eu

BASE="${TRIPSPLIT_BASE_URL:-http://localhost:5301}"
WORKDIR=$(mktemp -d)
SASHA_JAR="$WORKDIR/sasha.jar"
MASHA_JAR="$WORKDIR/masha.jar"

token() {
  grep -oE '__RequestVerificationToken"[^>]*value="[^"]*"' "$1" | head -1 | sed -E 's/.*value="([^"]*)".*/\1/'
}
guid_from() {
  echo "$1" | grep -oE '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}' | head -1
}
location_of() {
  grep -i '^location:' "$1" | sed -E 's/^[Ll]ocation: *//' | tr -d '\r\n'
}
participant_id() {
  # <option value="guid">Имя</option> или value="guid" .../><span>Имя</span>
  grep -oE "value=\"[0-9a-fA-F-]{36}\"[^>]*>$2</option>|value=\"[0-9a-fA-F-]{36}\"[^>]*>\\s*<span>$2</span>" "$1" \
    | grep -oE '[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}' | head -1
}

echo "== 1. Sasha: GET /Account/Login, POST /Account/Register =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Account/Login" -o "$WORKDIR/sasha-login.html"
T=$(token "$WORKDIR/sasha-login.html")
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" -D "$WORKDIR/sasha-reg.hdr" -o /dev/null \
  --data-urlencode "Register.Name=Sasha" \
  --data-urlencode "Register.Email=sasha@example.test" \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Account/Register"

echo "== 2. Masha: GET /Account/Login, POST /Account/Register =="
curl -s -c "$MASHA_JAR" -b "$MASHA_JAR" "$BASE/Account/Login" -o "$WORKDIR/masha-login.html"
T=$(token "$WORKDIR/masha-login.html")
curl -s -c "$MASHA_JAR" -b "$MASHA_JAR" -D "$WORKDIR/masha-reg.hdr" -o /dev/null \
  --data-urlencode "Register.Name=Masha" \
  --data-urlencode "Register.Email=masha@example.test" \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Account/Register"

echo "== 3. Sasha: GET /Trips, POST /Trips/Create =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Trips" -o "$WORKDIR/trips-index.html"
T=$(token "$WORKDIR/trips-index.html")
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" -D "$WORKDIR/trip-created.hdr" -o /dev/null \
  --data-urlencode "Create.Name=Weekend in Sochi" \
  --data-urlencode "Create.Currency=rub" \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Trips/Create"
TRIP_LOCATION=$(location_of "$WORKDIR/trip-created.hdr")
TRIP_ID=$(guid_from "$TRIP_LOCATION")
echo "   trip id = $TRIP_ID"

echo "== 4. Sasha: GET /Trips/Details/{id}, POST /Trips/Invite/{id} (Masha) =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Trips/Details/$TRIP_ID" -o "$WORKDIR/details1.html"
T=$(token "$WORKDIR/details1.html")
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" -D "$WORKDIR/invite.hdr" -o /dev/null \
  --data-urlencode "Invite.Email=masha@example.test" \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Trips/Invite/$TRIP_ID"
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Trips/Details/$TRIP_ID" -o "$WORKDIR/details2.html"
grep -q "Masha" "$WORKDIR/details2.html" && echo "   OK: Masha is now listed as a participant"

echo "== 5. Sasha: GET /Expenses/Create, POST /Expenses/Create (Dinner, 1000-200) =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Expenses/Create" -o "$WORKDIR/expense-create.html"
T=$(token "$WORKDIR/expense-create.html")
SASHA_ID=$(participant_id "$WORKDIR/expense-create.html" "Sasha")
MASHA_ID=$(participant_id "$WORKDIR/expense-create.html" "Masha")
echo "   sasha id = $SASHA_ID, masha id = $MASHA_ID"
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" -D "$WORKDIR/expense-created.hdr" -o /dev/null \
  --data-urlencode "Name=Dinner" \
  --data-urlencode "Type=Food" \
  --data-urlencode "Value=1000" \
  --data-urlencode "Discount=200" \
  --data-urlencode "PayerId=$SASHA_ID" \
  --data-urlencode "ConsumerIds=$SASHA_ID" \
  --data-urlencode "ConsumerIds=$MASHA_ID" \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Expenses/Create"

echo "== 6. Sasha: GET /Settlement (проверка перевода Masha -> Sasha) =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Settlement" -o "$WORKDIR/settlement.html"
grep -qE "400[.,]00" "$WORKDIR/settlement.html" && echo "   OK: 400 transfer amount found on the settlement page"

echo "== 7. Sasha: POST /Trips/Finish/{id} =="
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Trips/Details/$TRIP_ID" -o "$WORKDIR/before-finish.html"
T=$(token "$WORKDIR/before-finish.html")
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" -D "$WORKDIR/finish.hdr" -o /dev/null \
  --data-urlencode "__RequestVerificationToken=$T" \
  "$BASE/Trips/Finish/$TRIP_ID"
curl -s -c "$SASHA_JAR" -b "$SASHA_JAR" "$BASE/Trips/Details/$TRIP_ID" -o "$WORKDIR/after-finish.html"
grep -qi "status-finished" "$WORKDIR/after-finish.html" && echo "   OK: trip status is Finished"

echo "== 8. Sasha, Masha: POST /Account/Logout =="
for JAR_NAME in "$SASHA_JAR" "$MASHA_JAR"; do
  curl -s -c "$JAR_NAME" -b "$JAR_NAME" "$BASE/Trips" -o "$WORKDIR/pretrip.html"
  T=$(token "$WORKDIR/pretrip.html")
  curl -s -c "$JAR_NAME" -b "$JAR_NAME" -D "$WORKDIR/logout.hdr" -o /dev/null \
    --data-urlencode "__RequestVerificationToken=$T" \
    "$BASE/Account/Logout"
done

echo "== Done. Workdir: $WORKDIR =="
