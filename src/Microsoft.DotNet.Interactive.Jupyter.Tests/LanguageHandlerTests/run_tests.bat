@ECHO OFF
REM this script needs to be run in a conda environment where IPython and RScript are installed

ECHO "Running Python tests"
CALL "run_python_tests.bat"

ECHO "Running R tests"
CALL "run_r_tests.bat"
