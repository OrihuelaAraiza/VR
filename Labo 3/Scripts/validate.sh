#!/bin/zsh
set -euo pipefail

script_dir=${0:A:h}
project_dir=${script_dir:h}
derived_dir="${TMPDIR:-/tmp}/BancoVisionDerivedData"

cd "$project_dir"

required_files=(
  "BancoVision.xcodeproj/project.pbxproj"
  "BancoVision/BancoVisionApp.swift"
  "BancoVision/WorkbenchScene.swift"
  "BancoVisionTests/WorkbenchLayoutTests.swift"
  "DEVICE_TEST_CHECKLIST.md"
)

for required_file in "${required_files[@]}"; do
  if [[ ! -f "$required_file" ]]; then
    print -u2 "Falta: $required_file"
    exit 1
  fi
done

xcodebuild \
  -project BancoVision.xcodeproj \
  -scheme BancoVision \
  -sdk xrsimulator \
  -destination 'generic/platform=visionOS Simulator' \
  -derivedDataPath "$derived_dir" \
  CODE_SIGNING_ALLOWED=NO \
  build-for-testing

xcodebuild \
  -project BancoVision.xcodeproj \
  -scheme BancoVision \
  -sdk xros \
  -destination 'generic/platform=visionOS' \
  -derivedDataPath "$derived_dir-device" \
  CODE_SIGNING_ALLOWED=NO \
  build

print "VALIDACIÓN COMPLETA: simulador (build-for-testing) y dispositivo genérico (build)."
print "Pendiente por diseño: ejecutar y medir en un Apple Vision Pro físico."
