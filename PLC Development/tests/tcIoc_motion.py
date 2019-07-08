import os
import unittest

import sys

from parameterized import parameterized

from utils.emulator_launcher import CommandLineEmulatorLauncher
from utils.test_modes import TestModes
from utils.channel_access import ChannelAccess
from utils.ioc_launcher import get_default_ioc_dir, EPICS_TOP
from utils.testing import skip_if_recsim, get_running_lewis_and_ioc, parameterized_list
from time import sleep

# Device prefix
DEVICE_PREFIX = "TWINCAT_01"
EMULATOR_NAME = "PLC Development"

BECKHOFF_ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")

IOCS = [
    {
        "name": DEVICE_PREFIX,
        "directory": get_default_ioc_dir("TWINCAT"),
        "macros": {
            "TPY_FILE": "{}".format(os.path.join(BECKHOFF_ROOT, EMULATOR_NAME, "DrivePLC", "DrivePLC.tpy").replace(os.path.sep, "/"))
        },
        "emulator": EMULATOR_NAME,
        "emulator_launcher_class": CommandLineEmulatorLauncher,
        "emulator_command_line": "{} {} {}".format(
            os.path.join(BECKHOFF_ROOT, "util_scripts", "AutomationTools", "bin", "x64", "Release", "AutomationTools.exe"),
            os.path.join(BECKHOFF_ROOT, "PLC Development.sln"),
            "run"
        ),
        "emulator_wait_to_finish": True
    },
]


TEST_MODES = [TestModes.DEVSIM]


class tcIocTests(unittest.TestCase):

    def setUp(self):
        self._lewis, self._ioc = get_running_lewis_and_ioc(EMULATOR_NAME, DEVICE_PREFIX)

        self.ca = ChannelAccess(device_prefix="")
        self.ca.prefix = ""

        self.ca.set_pv_value("AXES_1:BENABLE", 1)
        self.ca.set_pv_value("AXES_1:FOVERRIDE", 100)

    def test_WHEN_IOC_running_THEN_cycle_counts_increasing(self):
        self.ca.assert_that_pv_value_is_increasing("AXES_1:AXIS-STATUS_CYCLECOUNTER", wait=1)

    def test_WHEN_moving_to_position_THEN_status_is_moving_and_gets_to_position(self):
        target = 10
        self.ca.set_pv_value("AXES_1:FPOSITION", target)
        self.ca.set_pv_value("AXES_1:FVELOCITY", 1)
        self.ca.assert_that_pv_is_number("AXES_1:IAXISSTATE", 200)  # Idle state
        self.ca.set_pv_value("AXES_1:ECOMMAND", 17)
        self.ca.set_pv_value("AXES_1:BEXECUTE", 1)
        self.ca.assert_that_pv_is_number("AXES_1:IAXISSTATE", 400)  # Moving state
        self.ca.assert_that_pv_value_is_increasing("AXES_1:AXIS-NCTOPLC_ACTPOS", wait=1)
        self.ca.assert_that_pv_is_number("AXES_1:AXIS-NCTOPLC_ACTPOS", target)
        self.ca.assert_that_pv_is_number("AXES_1:IAXISSTATE", 200)  # Idle state

    # def test_sleep(self):
	# 	sleep(100000)

