Beckhoff PLC base
------------------

Provides the ability to automatically clean/build/run Beckhoff PLC solutions as well as run PLC tests locally or in Jenkins.

To begin you must clone the PLC application to PLC_solutions:

`git PLC_REPOSITORY BECKHOFF_BASE_REPOSITORY\PLC_solution`

Currently scripts expect a name of `solution.sln` with a project name of `solution` as per the TwinCAT working groups project structure.

Tools
------

- Using `build.bat` will clean and build the automation tools as well as the PLC solution via TwinCAT XAE
- Using `run.bat` will locally run the PLC solution via TwinCAT XAE
- Jenkinsfile will allow a Jenkis pipeline to automate the building of the PLC and runing its tests
- Tests can be locally run using `python %EPICS_KIT_ROOT%\\support\\IocTestFramework\\master\\run_tests.py -tp ".\\PLC_solutionn\\tests"`
