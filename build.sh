#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

os_name=$(uname -s)
projects=""

if [[ "$os_name" != "Darwin" ]]; then
  echo "Not running on macOS. Excluding test projects."
  projects="--projects $(pwd)/src/Swift.Bindings/src/Swift.Bindings.csproj"
fi

"$scriptroot/eng/common/build.sh" --build --restore $projects $@
