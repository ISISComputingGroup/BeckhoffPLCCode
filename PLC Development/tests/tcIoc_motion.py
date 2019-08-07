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
        "emulator_command_line": '{} "{}" {}'.format(
            os.path.join(BECKHOFF_ROOT, "util_scripts", "AutomationTools", "bin", "x64", "Release", "AutomationTools.exe"),
            os.path.join(BECKHOFF_ROOT, "PLC Development.sln"),
            "run"
        ),
        "emulator_wait_to_finish": True
    },
]


TEST_MODES = [TestModes.DEVSIM]

IDLE_STATE = 200
MOVING_STATE = 400

GET_STATE = "AXES_1:IAXISSTATE"
MOTOR_SP = "MOT:MTR0101"
MOTOR_RBV = MOTOR_SP + ".RBV"
MOTOR_MOVING = MOTOR_SP + ".MOVN"
MOTOR_DONE = MOTOR_SP + ".DMOV"
MOTOR_DIR = MOTOR_SP + ".TDIR"
MOTOR_STOP = MOTOR_SP + ".STOP"


class tcIocTests(unittest.TestCase):

    def setUp(self):
        self._lewis, self._ioc = get_running_lewis_and_ioc(EMULATOR_NAME, DEVICE_PREFIX)

        self.bare_ca = ChannelAccess(device_prefix=None)
        self.bare_ca.prefix = ""

        self.motor_ca = ChannelAccess(device_prefix=None)

        self.bare_ca.set_pv_value("AXES_1:BENABLE", 1)
        self.bare_ca.set_pv_value("FWLIMIT_1", 1)
        self.bare_ca.set_pv_value("BWLIMIT_1", 1)
        self.bare_ca.set_pv_value("AXES_1:FOVERRIDE", 100)
        self.motor_ca.set_pv_value(MOTOR_SP, 0)
        self.motor_ca.assert_that_pv_is(MOTOR_DONE, 1, timeout=10)

    def test_WHEN_IOC_running_THEN_cycle_counts_increasing(self):
        self.bare_ca.assert_that_pv_value_is_increasing("AXES_1:AXIS-STATUS_CYCLECOUNTER", wait=1)

    def check_moving(self, expected_moving):
        self.motor_ca.assert_that_pv_is(MOTOR_MOVING, int(expected_moving), timeout=1)
        self.motor_ca.assert_that_pv_is(MOTOR_DONE, int(not expected_moving), timeout=1)
        self.bare_ca.assert_that_pv_is_number(GET_STATE, MOVING_STATE if expected_moving else IDLE_STATE)

    @parameterized.expand(
        parameterized_list([3.5, 6, -10])
    )
    def test_WHEN_moving_to_position_THEN_status_is_moving_and_gets_to_position(self, _, target):
        self.check_moving(False)
        self.motor_ca.set_pv_value(MOTOR_SP, target)
        self.check_moving(True)
        self.motor_ca.assert_that_pv_is(MOTOR_RBV, target, timeout=20)
        self.check_moving(False)

    def test_WHEN_moving_forward_THEN_motor_record_in_positive_direction(self):
        self.motor_ca.set_pv_value(MOTOR_SP, 2)
        self.motor_ca.assert_that_pv_is(MOTOR_DIR, 1)

    def test_WHEN_moving_backwards_THEN_motor_record_in_backwards_direction(self):
        self.motor_ca.set_pv_value(MOTOR_SP, -2)
        self.motor_ca.assert_that_pv_is(MOTOR_DIR, 0)

    @unittest.skipIf(True, 'Stop is not implemented in the PLC code')
    def test_WHEN_moving_THEN_can_stop_motion(self):
        self.motor_ca.set_pv_value(MOTOR_STOP, 1)
        self.motor_ca.set_pv_value(MOTOR_SP, 100)
        self.check_moving(False)
        self.motor_ca.assert_that_pv_is_not_number(MOTOR_RBV, 100, 10)
