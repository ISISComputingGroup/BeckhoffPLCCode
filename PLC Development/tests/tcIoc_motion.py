import os
import unittest

import sys

from parameterized import parameterized

from utils.emulator_launcher import BeckhoffEmulatorLauncher
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
            "TPY_FILE": "{}".format(os.path.join(BECKHOFF_ROOT, EMULATOR_NAME, "DrivePLC", "DrivePLC.tpy").replace(os.path.sep, "/")),
            "MTRCTRL": "1",
        },
        "emulator": EMULATOR_NAME,
        "emulator_launcher_class": BeckhoffEmulatorLauncher,
        "beckhoff_root": BECKHOFF_ROOT,
        "custom_prefix": "MOT",
        "pv_for_existence": "MTR0101",
    },
]


TEST_MODES = [TestModes.DEVSIM]

IDLE_STATE = 200
HOMING_STATE = 300
DISCRETE_MOTION_STATE = 400
CONTINUOUS_MOTION_STATE = 600

GET_STATE = "AXES_1:IAXISSTATE"
MOTOR_SP_BASE = "MOT:MTR010{}"
MOTOR_RBV_BASE = MOTOR_SP_BASE + ".RBV"

MOTOR_SP = MOTOR_SP_BASE.format(1)
MOTOR_RBV = MOTOR_RBV_BASE.format(1)
MOTOR_MOVING = MOTOR_SP + ".MOVN"
MOTOR_DONE = MOTOR_SP + ".DMOV"
MOTOR_DIR = MOTOR_SP + ".TDIR"
MOTOR_STOP = MOTOR_SP + ".STOP"
MOTOR_JOGF = MOTOR_SP + ".JOGF"
MOTOR_JOGR = MOTOR_SP + ".JOGR"

MOTOR_2_SP = MOTOR_SP_BASE.format(2)
MOTOR_2_RBV = MOTOR_RBV_BASE.format(2)


class TcIocTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        _, cls._ioc = get_running_lewis_and_ioc(EMULATOR_NAME, DEVICE_PREFIX)

        cls.bare_ca = ChannelAccess(device_prefix=None)

        cls.motor_ca = ChannelAccess(device_prefix=None)

        for i in range(1, 3):
            cls.bare_ca.set_pv_value("AXES_{}:BENABLE".format(i), 1)
            cls.bare_ca.set_pv_value("AXES_{}:FOVERRIDE".format(i), 100)

    def setUp(self):
        for i in range(1, 3):
            self.bare_ca.set_pv_value("FWLIMIT_{}".format(i), 1)
            self.bare_ca.set_pv_value("BWLIMIT_{}".format(i), 1)

        self.motor_ca.set_pv_value(MOTOR_2_SP, 0)

        self.motor_ca.set_pv_value(MOTOR_SP, 0)
        self.motor_ca.set_pv_value(MOTOR_SP + ".UEIP", 1)
        self.motor_ca.assert_that_pv_is(MOTOR_DONE, 1, timeout=10)

    def check_moving(self, expected_moving, moving_state=DISCRETE_MOTION_STATE):
        self.motor_ca.assert_that_pv_is(MOTOR_MOVING, int(expected_moving), timeout=1)
        self.motor_ca.assert_that_pv_is(MOTOR_DONE, int(not expected_moving), timeout=1)
        self.bare_ca.assert_that_pv_is_number(GET_STATE, moving_state if expected_moving else IDLE_STATE)

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

    @unittest.skipIf(True, 'Stop is not implemented for absolute moves in the PLC code')
    def test_WHEN_moving_THEN_can_stop_motion(self):
        self.motor_ca.set_pv_value(MOTOR_SP, 100)
        self.motor_ca.set_pv_value(MOTOR_STOP, 1)
        self.check_moving(False)
        self.motor_ca.assert_that_pv_is_not_number(MOTOR_RBV, 100, 10)

    @parameterized.expand(
        parameterized_list([(MOTOR_JOGR, 0), (MOTOR_JOGF, 1)])
    )
    def test_WHEN_jogging_THEN_can_stop_motion(self, _, pv, direction):
        self.motor_ca.set_pv_value(pv, 1)
        self.motor_ca.assert_that_pv_is(MOTOR_DIR, direction)
        self.check_moving(True, CONTINUOUS_MOTION_STATE)
        self.motor_ca.set_pv_value(MOTOR_STOP, 1)
        self.check_moving(False, CONTINUOUS_MOTION_STATE)

    @parameterized.expand(
        parameterized_list([3.5, 6, -10])
    )
    def test_WHEN_motor_2_moved_THEN_motor_2_gets_to_position_and_motor_1_not_moved(self, _, target):
        self.motor_ca.set_pv_value(MOTOR_2_SP, target)
        self.motor_ca.assert_that_pv_is(MOTOR_MOVING, 0)
        self.motor_ca.assert_that_pv_is_number(MOTOR_RBV, 0)
        self.motor_ca.assert_that_pv_is(MOTOR_2_RBV, target, timeout=20)

    @parameterized.expand(
        parameterized_list([(".HLS", "FWLIMIT_1"), (".LLS", "BWLIMIT_1")])
    )
    def test_WHEN_limits_hit_THEN_motor_reports_limits(self, _, motor_pv_suffix, pv_to_set):
        self.bare_ca.set_pv_value(pv_to_set, 0)
        self.motor_ca.assert_that_pv_is(MOTOR_SP + motor_pv_suffix, 1)

    @parameterized.expand(
        parameterized_list([3.5, 6, -10])
    )
    def test_WHEN_not_using_encoder_THEN_move_reaches_set_point(self, _, target):
        self.motor_ca.set_pv_value(MOTOR_SP + ".UEIP", 0)
        self.motor_ca.set_pv_value(MOTOR_SP, target)
        self.check_moving(True)
        self.motor_ca.assert_that_pv_is(MOTOR_RBV, target, timeout=20)

    def send_command(self, number):
        self.bare_ca.set_pv_value("AXES_1:ECOMMAND", number)
        self.bare_ca.set_pv_value("AXES_1:BEXECUTE", 1)

    def perform_dummy_home_routine(self):
        """
        This will perform a home routine via the backdoor, ideally this
        will be done in the PLC when a home command is sent but not currently simulated
        """
        self.send_command(43)  # Start homing
        sleep(1)  # Let the motor move a bit
        self.bare_ca.set_pv_value("AXES_1:MCSIGNALREF-LEVEL", 1)  # Simulate home signal
        self.send_command(40)  # Home finished

    def get_homed_bit(self):
        return int(self.motor_ca.get_pv_value(MOTOR_SP + ".MSTA")) & (1 << 14) != 0

    def test_WHEN_homing_sent_THEN_ends_in_homed_state(self):
        self.assertFalse(self.get_homed_bit())
        self.perform_dummy_home_routine()
        self.motor_ca.assert_that_pv_is(MOTOR_DONE, 1, timeout=10)
        self.assertTrue(self.get_homed_bit())
