#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
: "${ANDROID_HOME:?Set ANDROID_HOME to an Android SDK with platform 35 and build-tools 35.0.0}"
cd "$PROJECT_ROOT/android"
./gradlew --no-daemon :app:assembleDebug :app:lintDebug
mkdir -p "$PROJECT_ROOT/dist"
cp app/build/outputs/apk/debug/app-debug.apk "$PROJECT_ROOT/dist/dawn-chronicle-v0.1.0-debug.apk"
"$ANDROID_HOME/build-tools/35.0.0/apksigner" verify "$PROJECT_ROOT/dist/dawn-chronicle-v0.1.0-debug.apk"
cd "$PROJECT_ROOT/dist"
sha256sum dawn-chronicle-v0.1.0-debug.apk > dawn-chronicle-v0.1.0-debug.apk.sha256
