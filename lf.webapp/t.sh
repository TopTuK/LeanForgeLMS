set -euo pipefail
umask 077
POSTGRES_PASSWORD="p@ss=w/ord"; MINIO_ROOT_USER="m"; MINIO_ROOT_PASSWORD="mp"
DefaultAuth__JwtKey="k"; PmiAuth__ClientId="a"; PmiAuth__ClientSecret="b"; PmiAuth__OpenIdConfigurationUrl="u"
GoogleAuth__ClientId="g"; GoogleAuth__ClientSecret="gs"; YandexAuth__ClientId="y"; YandexAuth__ClientSecret="ys"
Robokassa__MerchantLogin="ml"; Robokassa__Password1="1"; Robokassa__Password2="2"
Unleash__ApiKey=""; SENTRY_DSN=""
POSTGRES_USER="leanforge"; POSTGRES_DB="leanforge"; WEBAPI_VIRTUAL_HOST="lms.s-sidorov.ru"; WEBAPI_HOST_PORT="8085"
UNLEASH_API_URL="https://features.s-sidorov.ru/api/"; Robokassa__HashAlgorithm="SHA256"; Robokassa__IsTest="false"
Robokassa__SuccessUrl="https://x/s"; Robokassa__FailUrl="https://x/f"
req(){ local value="${!1-}"; if [ -z "$value" ]; then echo "::error::required $1 not set"; exit 1; fi; printf '%s=%s\n' "$1" "$value"; }
opt(){ printf '%s=%s\n' "$1" "${!1-}"; }
{ echo "# header"; req POSTGRES_USER; req POSTGRES_PASSWORD; opt WEBAPI_HOST_PORT; req DefaultAuth__JwtKey; req Robokassa__Password1; opt SENTRY_DSN; opt Unleash__ApiKey; } > out.env
sed 's/\r$//' out.env > final.env
echo "entries: $(grep -c '=' final.env)"
cat final.env
