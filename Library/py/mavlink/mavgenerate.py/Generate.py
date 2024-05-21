'''
MAVLink protocol implementation (auto-generated by mavgen.py)

Generated from: mavlink.xml,common.xml,minimal.xml

Note: this file has been auto-generated. DO NOT EDIT
'''
from __future__ import print_function
from builtins import range
from builtins import object
import struct, array, time, json, os, sys, platform

from pymavlink.generator.mavcrc import x25crc
import hashlib

WIRE_PROTOCOL_VERSION = '2.0'
DIALECT = 'Generate'

PROTOCOL_MARKER_V1 = 0xFE
PROTOCOL_MARKER_V2 = 0xFD
HEADER_LEN_V1 = 6
HEADER_LEN_V2 = 10

MAVLINK_SIGNATURE_BLOCK_LEN = 13

MAVLINK_IFLAG_SIGNED = 0x01

native_supported = platform.system() != 'Windows' # Not yet supported on other dialects
native_force = 'MAVNATIVE_FORCE' in os.environ # Will force use of native code regardless of what client app wants
native_testing = 'MAVNATIVE_TESTING' in os.environ # Will force both native and legacy code to be used and their results compared

if native_supported and float(WIRE_PROTOCOL_VERSION) <= 1:
    try:
        import mavnative
    except ImportError:
        print('ERROR LOADING MAVNATIVE - falling back to python implementation')
        native_supported = False
else:
    # mavnative isn't supported for MAVLink2 yet
    native_supported = False

# allow MAV_IGNORE_CRC=1 to ignore CRC, allowing some
# corrupted msgs to be seen
MAVLINK_IGNORE_CRC = os.environ.get("MAV_IGNORE_CRC",0)

# some base types from mavlink_types.h
MAVLINK_TYPE_CHAR     = 0
MAVLINK_TYPE_UINT8_T  = 1
MAVLINK_TYPE_INT8_T   = 2
MAVLINK_TYPE_UINT16_T = 3
MAVLINK_TYPE_INT16_T  = 4
MAVLINK_TYPE_UINT32_T = 5
MAVLINK_TYPE_INT32_T  = 6
MAVLINK_TYPE_UINT64_T = 7
MAVLINK_TYPE_INT64_T  = 8
MAVLINK_TYPE_FLOAT    = 9
MAVLINK_TYPE_DOUBLE   = 10


# swiped from DFReader.py
def to_string(s):
    '''desperate attempt to convert a string regardless of what garbage we get'''
    try:
        return s.decode("utf-8")
    except Exception as e:
        pass
    try:
        s2 = s.encode('utf-8', 'ignore')
        x = u"%s" % s2
        return s2
    except Exception:
        pass
    # so it's a nasty one. Let's grab as many characters as we can
    r = ''
    try:
        for c in s:
            r2 = r + c
            r2 = r2.encode('ascii', 'ignore')
            x = u"%s" % r2
            r = r2
    except Exception:
        pass
    return r + '_XXX'


class MAVLink_header(object):
    '''MAVLink message header'''
    def __init__(self, msgId, incompat_flags=0, compat_flags=0, mlen=0, seq=0, srcSystem=0, srcComponent=0):
        self.mlen = mlen
        self.seq = seq
        self.srcSystem = srcSystem
        self.srcComponent = srcComponent
        self.msgId = msgId
        self.incompat_flags = incompat_flags
        self.compat_flags = compat_flags

    def pack(self, force_mavlink1=False):
        if WIRE_PROTOCOL_VERSION == '2.0' and not force_mavlink1:
            return struct.pack('<BBBBBBBHB', 253, self.mlen,
                               self.incompat_flags, self.compat_flags,
                               self.seq, self.srcSystem, self.srcComponent,
                               self.msgId&0xFFFF, self.msgId>>16)
        return struct.pack('<BBBBBB', PROTOCOL_MARKER_V1, self.mlen, self.seq,
                           self.srcSystem, self.srcComponent, self.msgId)

class MAVLink_message(object):
    '''base MAVLink message class'''
    def __init__(self, msgId, name):
        self._header     = MAVLink_header(msgId)
        self._payload    = None
        self._msgbuf     = None
        self._crc        = None
        self._fieldnames = []
        self._type       = name
        self._signed     = False
        self._link_id    = None
        self._instances  = None
        self._instance_field = None

    def format_attr(self, field):
        '''override field getter'''
        raw_attr = getattr(self,field)
        if isinstance(raw_attr, bytes):
            raw_attr = to_string(raw_attr).rstrip("\00")
        return raw_attr

    def get_msgbuf(self):
        if isinstance(self._msgbuf, bytearray):
            return self._msgbuf
        return bytearray(self._msgbuf)

    def get_header(self):
        return self._header

    def get_payload(self):
        return self._payload

    def get_crc(self):
        return self._crc

    def get_fieldnames(self):
        return self._fieldnames

    def get_type(self):
        return self._type

    def get_msgId(self):
        return self._header.msgId

    def get_srcSystem(self):
        return self._header.srcSystem

    def get_srcComponent(self):
        return self._header.srcComponent

    def get_seq(self):
        return self._header.seq

    def get_signed(self):
        return self._signed

    def get_link_id(self):
        return self._link_id

    def __str__(self):
        ret = '%s {' % self._type
        for a in self._fieldnames:
            v = self.format_attr(a)
            ret += '%s : %s, ' % (a, v)
        ret = ret[0:-2] + '}'
        return ret

    def __ne__(self, other):
        return not self.__eq__(other)

    def __eq__(self, other):
        if other is None:
            return False

        if self.get_type() != other.get_type():
            return False

        # We do not compare CRC because native code doesn't provide it
        #if self.get_crc() != other.get_crc():
        #    return False

        if self.get_seq() != other.get_seq():
            return False

        if self.get_srcSystem() != other.get_srcSystem():
            return False

        if self.get_srcComponent() != other.get_srcComponent():
            return False

        for a in self._fieldnames:
            if self.format_attr(a) != other.format_attr(a):
                return False

        return True

    def to_dict(self):
        d = dict({})
        d['mavpackettype'] = self._type
        for a in self._fieldnames:
          d[a] = self.format_attr(a)
        return d

    def to_json(self):
        return json.dumps(self.to_dict())

    def sign_packet(self, mav):
        h = hashlib.new('sha256')
        self._msgbuf += struct.pack('<BQ', mav.signing.link_id, mav.signing.timestamp)[:7]
        h.update(mav.signing.secret_key)
        h.update(self._msgbuf)
        sig = h.digest()[:6]
        self._msgbuf += sig
        mav.signing.timestamp += 1

    def pack(self, mav, crc_extra, payload, force_mavlink1=False):
        plen = len(payload)
        if WIRE_PROTOCOL_VERSION != '1.0' and not force_mavlink1:
            # in MAVLink2 we can strip trailing zeros off payloads. This allows for simple
            # variable length arrays and smaller packets
            nullbyte = chr(0)
            # in Python2, type("fred') is str but also type("fred")==bytes
            if str(type(payload)) == "<class 'bytes'>":
                nullbyte = 0
            while plen > 1 and payload[plen-1] == nullbyte:
                plen -= 1
        self._payload = payload[:plen]
        incompat_flags = 0
        if mav.signing.sign_outgoing:
            incompat_flags |= MAVLINK_IFLAG_SIGNED
        self._header  = MAVLink_header(self._header.msgId,
                                       incompat_flags=incompat_flags, compat_flags=0,
                                       mlen=len(self._payload), seq=mav.seq,
                                       srcSystem=mav.srcSystem, srcComponent=mav.srcComponent)
        self._msgbuf = self._header.pack(force_mavlink1=force_mavlink1) + self._payload
        crc = x25crc(self._msgbuf[1:])
        if True: # using CRC extra
            crc.accumulate_str(struct.pack('B', crc_extra))
        self._crc = crc.crc
        self._msgbuf += struct.pack('<H', self._crc)
        if mav.signing.sign_outgoing and not force_mavlink1:
            self.sign_packet(mav)
        return self._msgbuf

    def __getitem__(self, key):
        '''support indexing, allowing for multi-instance sensors in one message'''
        if self._instances is None:
            raise IndexError()
        if not key in self._instances:
            raise IndexError()
        return self._instances[key]


# enums

class EnumEntry(object):
    def __init__(self, name, description):
        self.name = name
        self.description = description
        self.param = {}

enums = {}

# MAV_RGB_STATUS
enums['MAV_RGB_STATUS'] = {}
MAV_RGB_STATUS_ON = 1 # 0x01.
enums['MAV_RGB_STATUS'][1] = EnumEntry('MAV_RGB_STATUS_ON', '''0x01.''')
MAV_RGB_STATUS_OFF = 2 # 0x02.
enums['MAV_RGB_STATUS'][2] = EnumEntry('MAV_RGB_STATUS_OFF', '''0x02.''')
MAV_RGB_STATUS_RGB = 4 # 0x04.
enums['MAV_RGB_STATUS'][4] = EnumEntry('MAV_RGB_STATUS_RGB', '''0x04.''')
MAV_RGB_STATUS_FLOW = 8 # 0x08.
enums['MAV_RGB_STATUS'][8] = EnumEntry('MAV_RGB_STATUS_FLOW', '''0x08.''')
MAV_RGB_STATUS_COLOR = 16 # 0x10.
enums['MAV_RGB_STATUS'][16] = EnumEntry('MAV_RGB_STATUS_COLOR', '''0x10.''')
MAV_RGB_STATUS_STRONG_WIND = 32 # 0x20.
enums['MAV_RGB_STATUS'][32] = EnumEntry('MAV_RGB_STATUS_STRONG_WIND', '''0x20.''')
MAV_RGB_STATUS_ENUM_END = 33 # 
enums['MAV_RGB_STATUS'][33] = EnumEntry('MAV_RGB_STATUS_ENUM_END', '''''')

# DIRECTION_OF_MOTION
enums['DIRECTION_OF_MOTION'] = {}
DIRECTION_FORWARD = 0 # forward.
enums['DIRECTION_OF_MOTION'][0] = EnumEntry('DIRECTION_FORWARD', '''forward.''')
DIRECTION_BACK = 1 # back.
enums['DIRECTION_OF_MOTION'][1] = EnumEntry('DIRECTION_BACK', '''back.''')
DIRECTION_LEFT = 2 # left.
enums['DIRECTION_OF_MOTION'][2] = EnumEntry('DIRECTION_LEFT', '''left.''')
DIRECTION_RIGHT = 3 # right.
enums['DIRECTION_OF_MOTION'][3] = EnumEntry('DIRECTION_RIGHT', '''right.''')
DIRECTION_OF_MOTION_ENUM_END = 4 # 
enums['DIRECTION_OF_MOTION'][4] = EnumEntry('DIRECTION_OF_MOTION_ENUM_END', '''''')

# CURVE_FLIGHT_MODE
enums['CURVE_FLIGHT_MODE'] = {}
FLIGHT_ELLIPTICAL_TRACE = 0 # elliptical trace.
enums['CURVE_FLIGHT_MODE'][0] = EnumEntry('FLIGHT_ELLIPTICAL_TRACE', '''elliptical trace.''')
FLIGHT_CICULAR_TRACE = 1 # circular trace.
enums['CURVE_FLIGHT_MODE'][1] = EnumEntry('FLIGHT_CICULAR_TRACE', '''circular trace.''')
FLIGHT_SPHE_SURFACE = 2 # spherical surface.
enums['CURVE_FLIGHT_MODE'][2] = EnumEntry('FLIGHT_SPHE_SURFACE', '''spherical surface.''')
CURVE_FLIGHT_MODE_ENUM_END = 3 # 
enums['CURVE_FLIGHT_MODE'][3] = EnumEntry('CURVE_FLIGHT_MODE_ENUM_END', '''''')

# MAV_FORMATION_CMD
enums['MAV_FORMATION_CMD'] = {}
MAV_FORMATION_CMD_TAKEOFF = 1 # Formation takeoff.
enums['MAV_FORMATION_CMD'][1] = EnumEntry('MAV_FORMATION_CMD_TAKEOFF', '''Formation takeoff.''')
MAV_FORMATION_CMD_LAND = 2 # Land.
enums['MAV_FORMATION_CMD'][2] = EnumEntry('MAV_FORMATION_CMD_LAND', '''Land.''')
enums['MAV_FORMATION_CMD'][2].param[1] = '''param1:empty. '''
enums['MAV_FORMATION_CMD'][2].param[2] = '''param2:empty. '''
enums['MAV_FORMATION_CMD'][2].param[3] = '''param3:empty. '''
enums['MAV_FORMATION_CMD'][2].param[4] = '''param4:1Byte:R value; 2Byte:G value; 3Byte:B value; 4Byte:see MAV_RGB_STATUS enum. '''
enums['MAV_FORMATION_CMD'][2].param[5] = '''x:empty '''
enums['MAV_FORMATION_CMD'][2].param[6] = '''y:empty. '''
enums['MAV_FORMATION_CMD'][2].param[7] = '''z:empty. '''
enums['MAV_FORMATION_CMD'][2].param[8] = '''yaw:empty. '''
MAV_FORMATION_CMD_PREPARE = 3 # Fromation fly prepare.
enums['MAV_FORMATION_CMD'][3] = EnumEntry('MAV_FORMATION_CMD_PREPARE', '''Fromation fly prepare.''')
MAV_FORMATION_CMD_ARM = 4 # Arm.
enums['MAV_FORMATION_CMD'][4] = EnumEntry('MAV_FORMATION_CMD_ARM', '''Arm.''')
MAV_FORMATION_CMD_DISARM = 5 # Disarm.
enums['MAV_FORMATION_CMD'][5] = EnumEntry('MAV_FORMATION_CMD_DISARM', '''Disarm.''')
MAV_FORMATION_CMD_TIME_SYNC = 6 # Fromation time sync.
enums['MAV_FORMATION_CMD'][6] = EnumEntry('MAV_FORMATION_CMD_TIME_SYNC', '''Fromation time sync.''')
MAV_FORMATION_CMD_AUX_SETUP = 7 # Fromation aux setup.
enums['MAV_FORMATION_CMD'][7] = EnumEntry('MAV_FORMATION_CMD_AUX_SETUP', '''Fromation aux setup.''')
enums['MAV_FORMATION_CMD'][7].param[1] = '''param1:empty. '''
enums['MAV_FORMATION_CMD'][7].param[2] = '''param2:empty. '''
enums['MAV_FORMATION_CMD'][7].param[3] = '''param3:empty. '''
enums['MAV_FORMATION_CMD'][7].param[4] = '''param4:empty. '''
enums['MAV_FORMATION_CMD'][7].param[5] = '''x:x local position of the 0 id pilot.(cm) '''
enums['MAV_FORMATION_CMD'][7].param[6] = '''y:y local position of the 0 id pilot.(cm). '''
enums['MAV_FORMATION_CMD'][7].param[7] = '''z:z local position of the 0 id pilot.(cm). '''
enums['MAV_FORMATION_CMD'][7].param[8] = '''yaw:real yaw.(0.01 degree) '''
MAV_FORMATION_CMD_ENTER_CALMAG = 8 # Enter cal mag.
enums['MAV_FORMATION_CMD'][8] = EnumEntry('MAV_FORMATION_CMD_ENTER_CALMAG', '''Enter cal mag.''')
MAV_FORMATION_CMD_EXIT_CALMAG = 9 # Exit cal mag.
enums['MAV_FORMATION_CMD'][9] = EnumEntry('MAV_FORMATION_CMD_EXIT_CALMAG', '''Exit cal mag.''')
MAV_FORMATION_CMD_SHOWON_WARNING_BATT = 10 # Show on warning battery.
enums['MAV_FORMATION_CMD'][10] = EnumEntry('MAV_FORMATION_CMD_SHOWON_WARNING_BATT', '''Show on warning battery.''')
MAV_FORMATION_CMD_SHOWOFF_WARNING_BATT = 11 # Show off warning battery.
enums['MAV_FORMATION_CMD'][11] = EnumEntry('MAV_FORMATION_CMD_SHOWOFF_WARNING_BATT', '''Show off warning battery.''')
MAV_FORMATION_CMD_ENABLE_LED = 12 # Enable led.
enums['MAV_FORMATION_CMD'][12] = EnumEntry('MAV_FORMATION_CMD_ENABLE_LED', '''Enable led.''')
MAV_FORMATION_CMD_DISABLE_LED = 13 # Disable led.
enums['MAV_FORMATION_CMD'][13] = EnumEntry('MAV_FORMATION_CMD_DISABLE_LED', '''Disable led.''')
MAV_FORMATION_CMD_ERASE_DANCE = 14 # Erase dance file.
enums['MAV_FORMATION_CMD'][14] = EnumEntry('MAV_FORMATION_CMD_ERASE_DANCE', '''Erase dance file.''')
MAV_FORMATION_CMD_REBOOT = 15 # Reboot.
enums['MAV_FORMATION_CMD'][15] = EnumEntry('MAV_FORMATION_CMD_REBOOT', '''Reboot.''')
MAV_FORMATION_CMD_ENABLE_RGB = 16 # Enable RGB.
enums['MAV_FORMATION_CMD'][16] = EnumEntry('MAV_FORMATION_CMD_ENABLE_RGB', '''Enable RGB.''')
MAV_FORMATION_CMD_DISABLE_RGB = 17 # Disable RGB.
enums['MAV_FORMATION_CMD'][17] = EnumEntry('MAV_FORMATION_CMD_DISABLE_RGB', '''Disable RGB.''')
MAV_FORMATION_CMD_ENABLE_FLOW = 18 # Enable light flow.
enums['MAV_FORMATION_CMD'][18] = EnumEntry('MAV_FORMATION_CMD_ENABLE_FLOW', '''Enable light flow.''')
MAV_FORMATION_CMD_DISABLE_FLOW = 19 # Disable light flow.
enums['MAV_FORMATION_CMD'][19] = EnumEntry('MAV_FORMATION_CMD_DISABLE_FLOW', '''Disable light flow.''')
MAV_FORMATION_CMD_CHANGE_ID = 20 # Change id.
enums['MAV_FORMATION_CMD'][20] = EnumEntry('MAV_FORMATION_CMD_CHANGE_ID', '''Change id.''')
MAV_FORMATION_CMD_ENTER_CALACC = 21 # Enter cal acc.
enums['MAV_FORMATION_CMD'][21] = EnumEntry('MAV_FORMATION_CMD_ENTER_CALACC', '''Enter cal acc.''')
MAV_FORMATION_CMD_EXIT_CALACC = 22 # Exit cal acc.
enums['MAV_FORMATION_CMD'][22] = EnumEntry('MAV_FORMATION_CMD_EXIT_CALACC', '''Exit cal acc.''')
MAV_FORMATION_CMD_ONE_KEY_TAKEOFF = 23 # One key takeoff.
enums['MAV_FORMATION_CMD'][23] = EnumEntry('MAV_FORMATION_CMD_ONE_KEY_TAKEOFF', '''One key takeoff.''')
enums['MAV_FORMATION_CMD'][23].param[1] = '''param1:takeoff alt. (cm) '''
enums['MAV_FORMATION_CMD'][23].param[2] = '''param2:empty. '''
enums['MAV_FORMATION_CMD'][23].param[3] = '''param3:empty. '''
enums['MAV_FORMATION_CMD'][23].param[4] = '''param4:1Byte:R value; 2Byte:G value; 3Byte:B value; 4Byte:see MAV_RGB_STATUS enum. '''
enums['MAV_FORMATION_CMD'][23].param[5] = '''x:empty. '''
enums['MAV_FORMATION_CMD'][23].param[6] = '''y:empty. '''
enums['MAV_FORMATION_CMD'][23].param[7] = '''z:empty. '''
enums['MAV_FORMATION_CMD'][23].param[8] = '''yaw:empty. '''
MAV_FORMATION_CMD_EXIT_TAKEOFF = 24 # Exit one key takeoff.
enums['MAV_FORMATION_CMD'][24] = EnumEntry('MAV_FORMATION_CMD_EXIT_TAKEOFF', '''Exit one key takeoff.''')
MAV_FORMATION_CMD_CHANGE_DANCE = 25 # Change dance file.
enums['MAV_FORMATION_CMD'][25] = EnumEntry('MAV_FORMATION_CMD_CHANGE_DANCE', '''Change dance file.''')
MAV_FORMATION_CMD_SET_RGB = 26 # Contor rgb light.
enums['MAV_FORMATION_CMD'][26] = EnumEntry('MAV_FORMATION_CMD_SET_RGB', '''Contor rgb light.''')
enums['MAV_FORMATION_CMD'][26].param[1] = '''param1:light seconds. '''
enums['MAV_FORMATION_CMD'][26].param[2] = '''param2:empty. '''
enums['MAV_FORMATION_CMD'][26].param[3] = '''param3:empty. '''