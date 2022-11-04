#!/usr/bin/bash

# Define directories.
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
BIN_DIR=$SCRIPT_DIR/bin
BUILD_DIR=$BIN_DIR/build
OUTPUT_DIR=$BIN_DIR/output

export OUTPUT="$OUTPUT_DIR"

# Define default arguments.
TARGET="all"
CONFIGURATION="Release"
SCRIPT_ARGUMENTS=()

# global function variables
PROJECT=""
PROJECT_DIR=""
PROJECT_FILE=""

VERSION=""
DOCKER_NAME=""
DOCKER_REPO=""
PUSH=False
LATEST=False

# Parse arguments.
for i in "$@"; do
  case $1 in
    -t|--target)
      TARGET="$2"
      shift # skip argument
      shift # skip value
      ;;
    -c|--configuration)
      CONFIGURATION="$2"
      shift # skip argument
      shift # skip value
      ;;
    -p|--push)
      PUSH=True
      shift # skip argument
      ;;
    -r|--repo)
      DOCKER_REPO="$2"
      shift # skip argument
      shift # skip value
      ;;
    -l|--latest)
      LATEST=True
      shift # skip argument
      ;;
    --)
      shift
      SCRIPT_ARGUMENTS+=("$@")
      break
      ;;
    *)
      SCRIPT_ARGUMENTS+=("$1")
      ;;
  esac
done

echo "Building Akka.Persistence.Sql data exporter"
echo "TARGET ......... : $TARGET"
echo "CONFIGURATION .. : $CONFIGURATION"
echo "REPO ........... : $DOCKER_REPO"
echo "PUSH ........... : $PUSH"
echo "LATEST ......... : $LATEST"

if [[ $TARGET != "all" && $TARGET != "mysql" && $TARGET != "postgresql" && $TARGET != "sqlserver" ]]; then
  echo "ERROR: Invalid -t|--target argument."
  echo "Target has to be either 'mysql', 'postgresql', 'sqlserver', or 'all'"
  exit 1
fi

if [[ $CONFIGURATION != "Release" && $CONFIGURATION != "Debug" ]]; then
  echo "ERROR: Invalid -c|--configuration argument."
  echo "Configuration has to be either 'Release' or 'Debug'"
  exit 1
fi

# Make sure folders exist.
if [ ! -d "$BIN_DIR" ]; then
  mkdir "$BIN_DIR"
fi
if [ ! -d "$BUILD_DIR" ]; then
  mkdir "$BUILD_DIR"
fi
if [ ! -d "$OUTPUT_DIR" ]; then
  mkdir "$OUTPUT_DIR"
fi

populate_var() {
  PROJECT=$1
  PROJECT_DIR="$SCRIPT_DIR/src/$PROJECT.Exporter"
  PROJECT_FILE="$PROJECT_DIR/$PROJECT.Exporter.csproj"

  echo "Grabbing Akka.Persistence.$PROJECT module version"

  local regex="/Akka.Persistence.$PROJECT/{print \$0}"
  local line
  line=$(awk "$regex" "$PROJECT_FILE")
  
  if [[ $line =~ ^.*Version=\"([a-zA-Z0-9.-_]*)\" ]]; then
	  VERSION=${BASH_REMATCH[1]}
  else
    echo "Failed to retrieve version number for Akka.Persistence.$PROJECT, could not find regex pattern"
	  exit 1
  fi

  local project_lower
  project_lower=$(echo "$PROJECT" | tr '[:upper:]' '[:lower:]')
  if [[ $DOCKER_REPO != "" ]]; then
    DOCKER_NAME="$DOCKER_REPO/akka-persistence-$project_lower-test-data"
  else
    DOCKER_NAME="akka-persistence-$project_lower-test-data"
  fi
}

build_project() {
  populate_var "$1"
  
  echo "Building $PROJECT exporter"
  if ! dotnet build -c "$CONFIGURATION" "$PROJECT_FILE" -o "$BUILD_DIR";
  then
	  exit 1
  fi
  
  echo "Executing $PROJECT exporter"
  cd "$BIN_DIR" || exit 1
  if ! dotnet "$BUILD_DIR/$PROJECT.Exporter.dll";
  then
    cd "$SCRIPT_DIR" || exit 1
    exit 1
  fi
  
  cd "$SCRIPT_DIR" || exit
}

build_docker_image() {
  populate_var "$1"
  
  echo "Building docker image: $DOCKER_NAME"

  cp "$PROJECT_DIR/Dockerfile" "$BIN_DIR/Dockerfile" || exit 1
  cd "$BIN_DIR" || exit 1
  
  if ! docker build -t "$DOCKER_NAME:$VERSION" .;
  then
    cd "$SCRIPT_DIR"  || exit 1
	  exit 1
  fi
  
  if [[ $LATEST = True ]]; then
    if ! docker image tag "$DOCKER_NAME:$VERSION" "$DOCKER_NAME:latest";
    then
      cd "$SCRIPT_DIR" || exit 1
	    exit 1
    fi
  fi
  
  if [[ $PUSH = True ]]; then
    if ! docker image push -a "$DOCKER_NAME";
    then
      cd "$SCRIPT_DIR" || exit 1
	    exit 1
    fi    
  fi
  
  cd "$SCRIPT_DIR" || exit 1
}

cleanup() {
  echo "Cleaning up temporary folders"
  if [[ -d "$BUILD_DIR" ]]; then
    echo "Cleaning $BUILD_DIR"
    rm -rf "${$BUILD_DIR:?}/"*
  fi
  if [[ -d "$OUTPUT_DIR" ]]; then
    echo "Cleaning $OUTPUT_DIR"
    rm -rf "${$OUTPUT_DIR:?}/"*
  fi
  if [[ -f "$BIN_DIR/Dockerfile" ]]; then
    echo "Removing $BIN_DIR/Dockerfile"
    rm -f "$BIN_DIR/Dockerfile"
  fi  
}

if [[ "sqlite" = "$TARGET" || "all" = "$TARGET" ]]; then
  build_project "Sqlite"
  # SqLite project outputs a file, not a docker image
  cp -f "$OUTPUT_DIR/database.db" "$BIN_DIR/akka-persistence-sqlite-test-data.$VERSION.db" || exit 1 
  cleanup
fi

if [[ "mysql" = "$TARGET" || "all" = "$TARGET" ]]; then
  build_project "MySql"
  build_docker_image "MySql"
  cleanup
fi

if [[ "postgresql" = "$TARGET" || "all" = "$TARGET" ]]; then
  build_project "PostgreSql"
  build_docker_image "PostgreSql"
  cleanup
fi

if [[ "sqlserver" = "$TARGET" || "all" = "$TARGET" ]]; then
  build_project "SqlServer"
  build_docker_image "SqlServer"
  cleanup
fi
