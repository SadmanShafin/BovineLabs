#!/bin/bash
# Generate all algorithm packages with package.json, asmdef, and skeleton runtime + test files

set -e
BASE="/home/l/Github/bovinelabs-core-internals/BovineLabs/Packages"

declare -A PKGS
PKGS=(
  ["com.bovinelabs.grid.anya"]="Anya|Anya any-angle pathfinding"
  ["com.bovinelabs.grid.fielddstar"]="FieldDStar|Field D* continuous shortest path"
  ["com.bovinelabs.grid.dstarlite"]="DStarLite|D* Lite dynamic shortest-path repair"
  ["com.bovinelabs.grid.sipp"]="Sipp|Safe Interval Path Planning"
  ["com.bovinelabs.grid.cbs"]="Cbs|Conflict-Based Search multi-agent"
  ["com.bovinelabs.grid.continuum"]="Continuum|Continuum Crowds density/flow"
  ["com.bovinelabs.grid.fastmarching"]="FastMarching|Fast Marching Method Eikonal solver"
  ["com.bovinelabs.grid.fastsweeping"]="FastSweeping|Fast Sweeping Method Eikonal solver"
  ["com.bovinelabs.grid.jps"]="Jps|Jump Point Search grid symmetry pruning"
  ["com.bovinelabs.grid.rsr"]="Rsr|Rectangular Symmetry Reduction"
  ["com.bovinelabs.grid.cpd"]="Cpd|Compressed Path Database"
  ["com.bovinelabs.grid.subgoal"]="Subgoal|Subgoal Graphs obstacle-corner abstraction"
  ["com.bovinelabs.grid.edt"]="Edt|Exact Euclidean Distance Transform"
  ["com.bovinelabs.grid.graphcut"]="GraphCut|Graph Cuts alpha-expansion"
  ["com.bovinelabs.grid.dynamiccut"]="DynamicCut|Dynamic Graph Cuts residual reuse"
  ["com.bovinelabs.grid.belief"]="Belief|Loopy Belief Propagation"
  ["com.bovinelabs.grid.watershed"]="Watershed|Watershed Transform"
  ["com.bovinelabs.grid.morse"]="Morse|Morse-Smale persistent watershed"
  ["com.bovinelabs.grid.thinning"]="Thinning|Topology-preserving thinning"
  ["com.bovinelabs.grid.wfc"]="Wfc|Wave Function Collapse"
  ["com.bovinelabs.grid.domino"]="Domino|Domino tiling via height functions"
  ["com.bovinelabs.grid.kasteleyn"]="Kasteleyn|Kasteleyn Pfaffian planar matching"
  ["com.bovinelabs.grid.wilson"]="Wilson|Wilson uniform spanning tree"
  ["com.bovinelabs.grid.cftp"]="Cftp|Coupling From The Past"
  ["com.bovinelabs.grid.hashlife"]="Hashlife|Hashlife cellular automata cache"
  ["com.bovinelabs.grid.sandpile"]="Sandpile|Abelian Sandpile chip-firing"
)

for pkg in "${!PKGS[@]}"; do
  IFS='|' read -r name desc <<< "${PKGS[$pkg]}"
  dir="$BASE/$pkg"
  mkdir -p "$dir/Runtime" "$dir/Tests"

  # package.json
  cat > "$dir/package.json" << EOF
{
  "name": "$pkg",
  "version": "0.1.0",
  "displayName": "BovineLabs Grid $name",
  "description": "$desc.",
  "unity": "6000.0",
  "dependencies": {
    "com.bovinelabs.grid": "0.1.0",
    "com.unity.collections": "2.0.0",
    "com.unity.mathematics": "1.0.0"
  }
}
EOF

  # Runtime asmdef
  cat > "$dir/Runtime/$pkg.asmdef" << EOF
{
  "name": "$pkg",
  "rootNamespace": "BovineLabs.Grid.$name",
  "references": ["com.bovinelabs.grid"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
EOF

  # Tests asmdef
  cat > "$dir/Tests/$pkg.tests.asmdef" << EOF
{
  "name": "$pkg.tests",
  "rootNamespace": "",
  "references": ["com.bovinelabs.grid", "$pkg"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
EOF

  echo "Created $pkg ($name)"
done

echo "Done generating all packages"
