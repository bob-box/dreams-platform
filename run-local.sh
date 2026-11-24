#!/bin/sh
set -eu

cd "$(dirname "$0")"
project=Borderland.Dreams.Web/Borderland.Dreams.Web.csproj
dotnet_cmd=${DOTNET_CMD:-dotnet}
local_dotnet="$HOME/.dotnet/dotnet"

if [ -z "${DOTNET_CMD:-}" ] && ! "$dotnet_cmd" --version >/dev/null 2>&1 && [ -x "$local_dotnet" ]; then
    dotnet_cmd="$local_dotnet"
fi

case "$dotnet_cmd" in
    /*) export DOTNET_ROOT="$(dirname "$dotnet_cmd")" ;;
esac

"$dotnet_cmd" build "$project"
exec "$dotnet_cmd" run --project "$project" --no-build --launch-profile https
