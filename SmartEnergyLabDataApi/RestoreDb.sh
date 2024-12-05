HOST=$1
FILE=$2
echo "$HOST"
echo "$FILE"
#psql -h "$HOST" -U smart_energy_lab < "$FILE"
pg_restore --clean --if-exists -v -h "$HOST" -U smart_energy_lab -d smart_energy_lab "$FILE"

