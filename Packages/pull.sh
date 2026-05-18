#!/bin/bash
# Pull all BovineLabs submodules on demand

cd "$(dirname "$0")"

if [ "$1" = "--remote" ]; then
	echo "Fetching latest from remote..."
	git submodule update --force --remote --init
else
	echo "Using pinned versions..."
	git submodule update --force --init --recursive
fi
