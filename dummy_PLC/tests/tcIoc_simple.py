import os
import unittest

import sys

from parameterized import parameterized

from utils.emulator_launcher import BeckhoffEmulatorLauncher
from utils.test_modes import TestModes
from utils.channel_access import ChannelAccess
from utils.ioc_launcher import get_default_ioc_dir, EPICS_TOP
from utils.testing import skip_if_recsim, get_running_lewis_and_ioc, parameterized_list

# Device prefix
DEVICE_PREFIX = "TWINCAT_01"
EMULATOR_NAME = "dummy_PLC"

BECKHOFF_ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")

IOCS = [
    {
        "name": DEVICE_PREFIX,
        "directory": get_default_ioc_dir("TWINCAT"),
        "macros": {
            "TPY_FILE": "{}".format(os.path.join(BECKHOFF_ROOT, EMULATOR_NAME, "TestPLC", "TestCode", "TestCode.tpy").replace(os.path.sep, "/"))
        },
        "emulator": EMULATOR_NAME,
        "emulator_launcher_class": BeckhoffEmulatorLauncher,
        "beckhoff_root": BECKHOFF_ROOT,
        "solution_path": os.path.join(EMULATOR_NAME, "TestPLC.sln"),
        "emulator_wait_to_finish": True
    },
]


TEST_MODES = [TestModes.DEVSIM]


class tcIocTests(unittest.TestCase):

    def setUp(self):
        self._lewis, self._ioc = get_running_lewis_and_ioc(EMULATOR_NAME, DEVICE_PREFIX)

        self.ca = ChannelAccess(device_prefix=None)

    def test_WHEN_var_one_and_two_written_to_THEN_output_is_sum(self):
        self.ca.set_pv_value("ITEST1", 10)
        self.ca.set_pv_value("ITEST2", 20)
        self.ca.assert_that_pv_is("ISUM", 30)

    @parameterized.expand(parameterized_list(["ENUM_VALUE_0", "ENUM_VALUE_1"]))
    def test_WHEN_enum_written_to_THEN_enum_readback_is_correct(self, _, enum):
        self.ca.set_pv_value("ETESTENUM", enum)
        self.ca.assert_that_pv_is("ETESTENUMRBV", enum)
