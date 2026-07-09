#!/usr/bin/env bash
# Quick project checks for UltimateTicTacToe
# Run from the project root (where the .slnx files live):
#   ./tools/check_project.sh

set -u
ROOT_DIR="$(pwd)"
echo "Running quick checks in $ROOT_DIR"
echo

# Helper for printing separators
sep() { echo; echo "-----------------------------"; echo; }

# 1) Check for dotnet and attempt a build if available
sep
if command -v dotnet >/dev/null 2>&1; then
  echo "dotnet found: running 'dotnet build' on UltimateTicTacToe.slnx (no restore)" 
  dotnet build UltimateTicTacToe.slnx || echo "dotnet build returned non-zero exit code"
else
  echo "dotnet not found — skipping dotnet build. Install .NET SDK to enable full compile checks."
fi

# 2) Grep checks for common Unity/C# issues
sep
echo "Searching C# sources for potential issues..."

# a) Multidimensional array usages like [9, 9]
echo "Potential multidimensional array initializers (e.g. new Type[9, 9]):"
egrep -RIn --include="*.cs" '\[[^]]*,[^]]*\]' Assets || true

# b) globalBoardData usages
echo
echo "Occurrences of 'globalBoardData[' (verify flattened usage):"
grep -RIn --include="*.cs" "globalBoardData\[" Assets || true

# c) Deprecated FindObjectsByType overloads using FindObjectsSortMode
echo
echo "Deprecated FindObjectsByType(...FindObjectsSortMode) usages:"
grep -RIn --include="*.cs" "FindObjectsByType.*FindObjectsSortMode" Assets || true

# d) Any FindObjectsOfType / FindObjectOfType usages (scan for potential deprecated patterns)
echo
echo "FindObjectsOfType/FindObjectOfType occurrences (review for overload changes):"
grep -RIn --include="*.cs" "FindObjectsOfType\(|FindObjectOfType\(" Assets || true

# e) Direct accesses to 'isOccupied' (should be private on Cell)
echo
echo "Direct .isOccupied usages (should be avoided; only in cell.cs):"
grep -RIn --include="*.cs" "\.isOccupied" Assets | grep -v "Board/cell.cs" || true

# f) Obsolete attribute warnings scanning (simple heuristic for 'Obsolete' usage)
echo
echo "[Info] 'Obsolete' attributes in code (may indicate changed APIs):"
grep -RIn --include="*.cs" "\[Obsolete" Assets || true

sep
echo "Quick checks completed. Review output above for warnings or suspicious matches."

exit 0
