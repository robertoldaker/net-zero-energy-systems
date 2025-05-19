#!/bin/bash

# --- Configuration ---
URL="https://lv-data.net-zero-energy-systems.org/LoadProfiles/GridSupplyPointLoadProfiles?id=86&source=3&year=2021" # Website URL to check (passed as the first argument)
EMAIL_TO="roldaker@gmail.com" # Email address to send error notifications to
EMAIL_SUBJECT="Website Check Failed"
EMAIL_FROM="admin@net-zero-energy-systems.org"
SMTP_SERVER="127.0.0.1"   # Your SMTP server address (e.g., smtp.gmail.com)
SMTP_PORT="25"                # Your SMTP server port (e.g., 587 for TLS)
SMTP_USER=""   # Your SMTP username (if required)
SMTP_PASSWORD="" # Your SMTP password (if required)

LOG_FILE="/tmp/website_check.log" # Optional log file

# --- Helper Functions ---

log_message() {
  local timestamp=$(date +"%Y-%m-%d %H:%M:%S")
  echo "$timestamp: $1" >> "$LOG_FILE"
  echo "$timestamp: $1" # Also output to console
}

send_email() {
  local subject="$1"
  local body="$2"

  log_message "Sending email notification to $EMAIL_TO..."

  # Using 'mail' command (you might need to install it: sudo apt-get install mailutils)
  echo "$body" | mail -s "$subject" "$EMAIL_TO" -aFrom:$EMAIL_FROM

  # Alternative using 'sendmail' (often available by default)
  # echo "Subject: $subject" | cat - "$body" | sendmail "$EMAIL_TO"

  # Alternative using 'swaks' (more options, install if needed: sudo apt-get install swaks)
  # swaks --to "$EMAIL_TO" --from "$SMTP_USER" --server "$SMTP_SERVER:$SMTP_PORT" --auth LOGIN --auth-user "$SMTP_USER" --auth-password "$SMTP_PASSWORD" --subject "$subject" --body "$body" --tls

  if [ $? -eq 0 ]; then
    log_message "Email sent successfully."
  else
    log_message "Error sending email."
  fi
}

# --- Main Script ---

if [ -z "$URL" ]; then
  echo "Error: Please provide the website URL as the first argument."
  exit 1
fi

log_message "Checking website: $URL"

# Perform the GET request and capture the response code
response_code=$(curl -s -o /dev/null -w "%{http_code}" "$URL")

log_message "HTTP Response Code: $response_code"

# Check for error conditions (non-2xx or 3xx status codes are often considered errors)
if [[ ! "$response_code" =~ ^(2|3)[0-9]{2}$ ]]; then
  error_message="Website check failed. HTTP Response Code: $response_code for URL: $URL"
  log_message "$error_message"
  send_email "$EMAIL_SUBJECT" "$error_message"
else
  log_message "Website check successful (HTTP ${response_code})."
fi

exit 0
