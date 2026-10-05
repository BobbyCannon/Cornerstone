#include "KeyTransform.h"

#import <Carbon/Carbon.h>
#include <array>
#include <unordered_map>

struct KeyInfo
{
    uint16_t scanCode;
    CsnPhysicalKey physicalKey;
    CsnKey qwertyKey;
    uint16_t menuChar;
};

// ScanCode - PhysicalKey - Key mapping (the virtual key is mapped as in a standard QWERTY keyboard)
// https://github.com/chromium/chromium/blob/main/ui/events/keycodes/dom/dom_code_data.inc
// This list has the same order as the PhysicalKey enum.
const KeyInfo keyInfos[] =
{
    // Writing System Keys
    { 0x32, CsnPhysicalKeyBackquote, CsnKeyOem3, '`' },
    { 0x2A, CsnPhysicalKeyBackslash, CsnKeyOem5, '\\' },
    { 0x21, CsnPhysicalKeyBracketLeft,CsnKeyOem4, '[' },
    { 0x1E, CsnPhysicalKeyBracketRight, CsnKeyOem6, ']' },
    { 0x2B, CsnPhysicalKeyComma, CsnKeyOemComma, ',' },
    { 0x1D, CsnPhysicalKeyDigit0, CsnKeyD0, '0' },
    { 0x12, CsnPhysicalKeyDigit1, CsnKeyD1, '1' },
    { 0x13, CsnPhysicalKeyDigit2, CsnKeyD2, '2' },
    { 0x14, CsnPhysicalKeyDigit3, CsnKeyD3, '3' },
    { 0x15, CsnPhysicalKeyDigit4, CsnKeyD4, '4' },
    { 0x17, CsnPhysicalKeyDigit5, CsnKeyD5, '5' },
    { 0x16, CsnPhysicalKeyDigit6, CsnKeyD6, '6' },
    { 0x1A, CsnPhysicalKeyDigit7, CsnKeyD7, '7' },
    { 0x1C, CsnPhysicalKeyDigit8, CsnKeyD8, '8' },
    { 0x19, CsnPhysicalKeyDigit9, CsnKeyD9, '9' },
    { 0x18, CsnPhysicalKeyEqual, CsnKeyOemPlus, '=' },
    { 0x0A, CsnPhysicalKeyIntlBackslash, CsnKeyOem102, 0 },
    { 0x5E, CsnPhysicalKeyIntlRo, CsnKeyOem102, 0 },
    { 0x5D, CsnPhysicalKeyIntlYen, CsnKeyOem5, 0 },
    { 0x00, CsnPhysicalKeyA, CsnKeyA, 'a' },
    { 0x0B, CsnPhysicalKeyB, CsnKeyB, 'b' },
    { 0x08, CsnPhysicalKeyC, CsnKeyC, 'c' },
    { 0x02, CsnPhysicalKeyD, CsnKeyD, 'd' },
    { 0x0E, CsnPhysicalKeyE, CsnKeyE, 'e' },
    { 0x03, CsnPhysicalKeyF, CsnKeyF, 'f' },
    { 0x05, CsnPhysicalKeyG, CsnKeyG, 'g' },
    { 0x04, CsnPhysicalKeyH, CsnKeyH, 'h' },
    { 0x22, CsnPhysicalKeyI, CsnKeyI, 'i' },
    { 0x26, CsnPhysicalKeyJ, CsnKeyJ, 'j' },
    { 0x28, CsnPhysicalKeyK, CsnKeyK, 'k' },
    { 0x25, CsnPhysicalKeyL, CsnKeyL, 'l' },
    { 0x2E, CsnPhysicalKeyM, CsnKeyM, 'm' },
    { 0x2D, CsnPhysicalKeyN, CsnKeyN, 'n' },
    { 0x1F, CsnPhysicalKeyO, CsnKeyO, 'o' },
    { 0x23, CsnPhysicalKeyP, CsnKeyP, 'p' },
    { 0x0C, CsnPhysicalKeyQ, CsnKeyQ, 'q' },
    { 0x0F, CsnPhysicalKeyR, CsnKeyR, 'r' },
    { 0x01, CsnPhysicalKeyS, CsnKeyS, 's' },
    { 0x11, CsnPhysicalKeyT, CsnKeyT, 't' },
    { 0x20, CsnPhysicalKeyU, CsnKeyU, 'u' },
    { 0x09, CsnPhysicalKeyV, CsnKeyV, 'v' },
    { 0x0D, CsnPhysicalKeyW, CsnKeyW, 'w' },
    { 0x07, CsnPhysicalKeyX, CsnKeyX, 'x' },
    { 0x10, CsnPhysicalKeyY, CsnKeyY, 'y' },
    { 0x06, CsnPhysicalKeyZ, CsnKeyZ, 'z' },
    { 0x1B, CsnPhysicalKeyMinus, CsnKeyOemMinus, '-' },
    { 0x2F, CsnPhysicalKeyPeriod, CsnKeyOemPeriod, '.' },
    { 0x27, CsnPhysicalKeyQuote, CsnKeyOem7, '\'' },
    { 0x29, CsnPhysicalKeySemicolon, CsnKeyOem1, ';' },
    { 0x2C, CsnPhysicalKeySlash, CsnKeyOem2, '/' },

    // Functional Keys
    { 0x3A, CsnPhysicalKeyAltLeft, CsnKeyLeftAlt, 0 },
    { 0x3D, CsnPhysicalKeyAltRight, CsnKeyRightAlt, 0 },
    { 0x33, CsnPhysicalKeyBackspace, CsnKeyBack, kBackspaceCharCode },
    { 0x39, CsnPhysicalKeyCapsLock, CsnKeyCapsLock, 0 },
    { 0x6E, CsnPhysicalKeyContextMenu, CsnKeyApps, 0 },
    { 0x3B, CsnPhysicalKeyControlLeft, CsnKeyLeftCtrl, 0 },
    { 0x3E, CsnPhysicalKeyControlRight, CsnKeyRightCtrl, 0 },
    { 0x24, CsnPhysicalKeyEnter, CsnKeyEnter, kReturnCharCode },
    { 0x37, CsnPhysicalKeyMetaLeft, CsnKeyLWin, 0 },
    { 0x36, CsnPhysicalKeyMetaRight, CsnKeyRWin, 0 },
    { 0x38, CsnPhysicalKeyShiftLeft, CsnKeyLeftShift, 0 },
    { 0x3C, CsnPhysicalKeyShiftRight, CsnKeyRightShift, 0 },
    { 0x31, CsnPhysicalKeySpace, CsnKeySpace, kSpaceCharCode },
    { 0x30, CsnPhysicalKeyTab, CsnKeyTab, kTabCharCode },
    //{   , CsnPhysicalKeyConvert, 0 },
    //{   , CsnPhysicalKeyKanaMode, 0 },
    { 0x68, CsnPhysicalKeyLang1, CsnKeyKanaMode, 0 },
    { 0x66, CsnPhysicalKeyLang2, CsnKeyHanjaMode, 0 },
    //{   , CsnPhysicalKeyLang3, 0 },
    //{   , CsnPhysicalKeyLang4, 0 },
    //{   , CsnPhysicalKeyLang5, 0 },
    //{   , CsnPhysicalKeyNonConvert, 0 },

    // Control Pad Section
    { 0x75, CsnPhysicalKeyDelete, CsnKeyDelete, NSDeleteFunctionKey },
    { 0x77, CsnPhysicalKeyEnd, CsnKeyEnd, NSEndFunctionKey },
    //{   , CsnPhysicalKeyHelp, 0 },
    { 0x73, CsnPhysicalKeyHome, CsnKeyHome, NSHomeFunctionKey },
    { 0x72, CsnPhysicalKeyInsert, CsnKeyInsert, NSInsertFunctionKey },
    { 0x79, CsnPhysicalKeyPageDown, CsnKeyPageDown, NSPageDownFunctionKey },
    { 0x74, CsnPhysicalKeyPageUp, CsnKeyPageUp, NSPageUpFunctionKey },

    // Arrow Pad Section
    { 0x7D, CsnPhysicalKeyArrowDown, CsnKeyDown, NSDownArrowFunctionKey },
    { 0x7B, CsnPhysicalKeyArrowLeft, CsnKeyLeft, NSLeftArrowFunctionKey },
    { 0x7C, CsnPhysicalKeyArrowRight, CsnKeyRight, NSRightArrowFunctionKey },
    { 0x7E, CsnPhysicalKeyArrowUp, CsnKeyUp, NSUpArrowFunctionKey },

    // Numpad Section
    { 0x47, CsnPhysicalKeyNumLock, CsnKeyClear, kClearCharCode },
    { 0x52, CsnPhysicalKeyNumPad0, CsnKeyNumPad0, '0' },
    { 0x53, CsnPhysicalKeyNumPad1, CsnKeyNumPad1, '1' },
    { 0x54, CsnPhysicalKeyNumPad2, CsnKeyNumPad2, '2' },
    { 0x55, CsnPhysicalKeyNumPad3, CsnKeyNumPad3, '3' },
    { 0x56, CsnPhysicalKeyNumPad4, CsnKeyNumPad4, '4' },
    { 0x57, CsnPhysicalKeyNumPad5, CsnKeyNumPad5, '5' },
    { 0x58, CsnPhysicalKeyNumPad6, CsnKeyNumPad6, '6' },
    { 0x59, CsnPhysicalKeyNumPad7, CsnKeyNumPad7, '7' },
    { 0x5B, CsnPhysicalKeyNumPad8, CsnKeyNumPad8, '8' },
    { 0x5C, CsnPhysicalKeyNumPad9, CsnKeyNumPad9, '9' },
    { 0x45, CsnPhysicalKeyNumPadAdd, CsnKeyAdd, '+' },
    //{   , CsnPhysicalKeyNumPadClear, 0 },
    { 0x5F, CsnPhysicalKeyNumPadComma, CsnKeyAbntC2, 0 },
    { 0x41, CsnPhysicalKeyNumPadDecimal, CsnKeyDecimal, '.' },
    { 0x4B, CsnPhysicalKeyNumPadDivide, CsnKeyDivide, '/' },
    { 0x4C, CsnPhysicalKeyNumPadEnter, CsnKeyEnter, kReturnCharCode },
    { 0x51, CsnPhysicalKeyNumPadEqual, CsnKeyOemPlus, '=' },
    { 0x43, CsnPhysicalKeyNumPadMultiply, CsnKeyMultiply, '*' },
    //{   , CsnPhysicalKeyNumPadParenLeft, 0 },
    //{   , CsnPhysicalKeyNumPadParenRight, 0 },
    { 0x4E, CsnPhysicalKeyNumPadSubtract, CsnKeySubtract, '-' },

    // Function Section
    { 0x35, CsnPhysicalKeyEscape, CsnKeyEscape, kEscapeCharCode },
    { 0x7A, CsnPhysicalKeyF1, CsnKeyF1, NSF1FunctionKey },
    { 0x78, CsnPhysicalKeyF2, CsnKeyF2, NSF2FunctionKey },
    { 0x63, CsnPhysicalKeyF3, CsnKeyF3, NSF3FunctionKey },
    { 0x76, CsnPhysicalKeyF4, CsnKeyF4, NSF4FunctionKey },
    { 0x60, CsnPhysicalKeyF5, CsnKeyF5, NSF5FunctionKey },
    { 0x61, CsnPhysicalKeyF6, CsnKeyF6, NSF6FunctionKey },
    { 0x62, CsnPhysicalKeyF7, CsnKeyF7, NSF7FunctionKey },
    { 0x64, CsnPhysicalKeyF8, CsnKeyF8, NSF8FunctionKey },
    { 0x65, CsnPhysicalKeyF9, CsnKeyF9, NSF9FunctionKey },
    { 0x6D, CsnPhysicalKeyF10, CsnKeyF10, NSF10FunctionKey },
    { 0x67, CsnPhysicalKeyF11, CsnKeyF11, NSF11FunctionKey },
    { 0x6F, CsnPhysicalKeyF12, CsnKeyF12, NSF12FunctionKey },
    { 0x69, CsnPhysicalKeyF13, CsnKeyF13, NSF13FunctionKey },
    { 0x6B, CsnPhysicalKeyF14, CsnKeyF14, NSF14FunctionKey },
    { 0x71, CsnPhysicalKeyF15, CsnKeyF15, NSF15FunctionKey },
    { 0x6A, CsnPhysicalKeyF16, CsnKeyF16, NSF16FunctionKey },
    { 0x40, CsnPhysicalKeyF17, CsnKeyF17, NSF17FunctionKey },
    { 0x4F, CsnPhysicalKeyF18, CsnKeyF18, NSF18FunctionKey },
    { 0x50, CsnPhysicalKeyF19, CsnKeyF19, NSF19FunctionKey },
    { 0x5A, CsnPhysicalKeyF20, CsnKeyF20, NSF20FunctionKey },
    //{   , CsnPhysicalKeyF21, 0 },
    //{   , CsnPhysicalKeyF22, 0 },
    //{   , CsnPhysicalKeyF23, 0 },
    //{   , CsnPhysicalKeyF24, 0 },
    //{   , CsnPhysicalKeyPrintScreen, 0 },
    //{   , CsnPhysicalKeyScrollLock, 0 },
    //{   , CsnPhysicalKeyPause, 0 },

    // Media Keys
    //{   , CsnPhysicalKeyBrowserBack, 0 },
    //{   , CsnPhysicalKeyBrowserFavorites, 0 },
    //{   , CsnPhysicalKeyBrowserForward, 0 },
    //{   , CsnPhysicalKeyBrowserHome, 0 },
    //{   , CsnPhysicalKeyBrowserRefresh, 0 },
    //{   , CsnPhysicalKeyBrowserSearch, 0 },
    //{   , CsnPhysicalKeyBrowserStop, 0 },
    //{   , CsnPhysicalKeyEject, 0 },
    //{   , CsnPhysicalKeyLaunchApp1, 0 },
    //{   , CsnPhysicalKeyLaunchApp2, 0 },
    //{   , CsnPhysicalKeyLaunchMail, 0 },
    //{   , CsnPhysicalKeyMediaPlayPause, 0 },
    //{   , CsnPhysicalKeyMediaSelect, 0 },
    //{   , CsnPhysicalKeyMediaStop, 0 },
    //{   , CsnPhysicalKeyMediaTrackNext, 0 },
    //{   , CsnPhysicalKeyMediaTrackPrevious, 0 },
    //{   , CsnPhysicalKeyPower, 0 },
    //{   , CsnPhysicalKeySleep, 0 },
    { 0x49, CsnPhysicalKeyAudioVolumeDown, CsnKeyVolumeDown, 0 },
    { 0x4A, CsnPhysicalKeyAudioVolumeMute, CsnKeyVolumeMute, 0 },
    { 0x48, CsnPhysicalKeyAudioVolumeUp, CsnKeyVolumeUp, 0 },
    //{   , CsnPhysicalKeyWakeUp, 0 },

    // Legacy Keys
    //{   , CsnPhysicalKeyAgain, 0 },
    //{   , CsnPhysicalKeyCopy, 0 },
    //{   , CsnPhysicalKeyCut, 0 },
    //{   , CsnPhysicalKeyFind, 0 },
    //{   , CsnPhysicalKeyOpen, 0 },
    //{   , CsnPhysicalKeyPaste, 0 },
    //{   , CsnPhysicalKeyProps, 0 },
    //{   , CsnPhysicalKeySelect, 0 },
    //{   , CsnPhysicalKeyUndo, 0 }
};

std::unordered_map<uint16_t, CsnKey> virtualKeyFromChar =
{
    // Alphabetic keys
    { 'A', CsnKeyA },
    { 'B', CsnKeyB },
    { 'C', CsnKeyC },
    { 'D', CsnKeyD },
    { 'E', CsnKeyE },
    { 'F', CsnKeyF },
    { 'G', CsnKeyG },
    { 'H', CsnKeyH },
    { 'I', CsnKeyI },
    { 'J', CsnKeyJ },
    { 'K', CsnKeyK },
    { 'L', CsnKeyL },
    { 'M', CsnKeyM },
    { 'N', CsnKeyN },
    { 'O', CsnKeyO },
    { 'P', CsnKeyP },
    { 'Q', CsnKeyQ },
    { 'R', CsnKeyR },
    { 'S', CsnKeyS },
    { 'T', CsnKeyT },
    { 'U', CsnKeyU },
    { 'V', CsnKeyV },
    { 'W', CsnKeyW },
    { 'X', CsnKeyX },
    { 'Y', CsnKeyY },
    { 'Z', CsnKeyZ },
    { 'a', CsnKeyA },
    { 'b', CsnKeyB },
    { 'c', CsnKeyC },
    { 'd', CsnKeyD },
    { 'e', CsnKeyE },
    { 'f', CsnKeyF },
    { 'g', CsnKeyG },
    { 'h', CsnKeyH },
    { 'i', CsnKeyI },
    { 'j', CsnKeyJ },
    { 'k', CsnKeyK },
    { 'l', CsnKeyL },
    { 'm', CsnKeyM },
    { 'n', CsnKeyN },
    { 'o', CsnKeyO },
    { 'p', CsnKeyP },
    { 'q', CsnKeyQ },
    { 'r', CsnKeyR },
    { 's', CsnKeyS },
    { 't', CsnKeyT },
    { 'u', CsnKeyU },
    { 'v', CsnKeyV },
    { 'w', CsnKeyW },
    { 'x', CsnKeyX },
    { 'y', CsnKeyY },
    { 'z', CsnKeyZ },

    // Punctuation: US specific mappings (same as Chromium)
    { ';', CsnKeyOem1 },
    { ':', CsnKeyOem1 },
    { '=', CsnKeyOemPlus },
    { '+', CsnKeyOemPlus },
    { ',', CsnKeyOemComma },
    { '<', CsnKeyOemComma },
    { '-', CsnKeyOemMinus },
    { '_', CsnKeyOemMinus },
    { '.', CsnKeyOemPeriod },
    { '>', CsnKeyOemPeriod },
    { '/', CsnKeyOem2 },
    { '?', CsnKeyOem2 },
    { '`', CsnKeyOem3 },
    { '~', CsnKeyOem3 },
    { '[', CsnKeyOem4 },
    { '{', CsnKeyOem4 },
    { '\\', CsnKeyOem5 },
    { '|', CsnKeyOem5 },
    { ']', CsnKeyOem6 },
    { '}', CsnKeyOem6 },
    { '\'', CsnKeyOem7 },
    { '"', CsnKeyOem7 },

    // Apple function keys
    // https://developer.apple.com/documentation/appkit/1535851-function-key_unicode_values
    { NSDeleteFunctionKey, CsnKeyDelete },
    { NSUpArrowFunctionKey, CsnKeyUp },
    { NSLeftArrowFunctionKey, CsnKeyLeft },
    { NSRightArrowFunctionKey, CsnKeyRight },
    { NSPageUpFunctionKey, CsnKeyPageUp },
    { NSPageDownFunctionKey, CsnKeyPageDown },
    { NSHomeFunctionKey, CsnKeyHome },
    { NSEndFunctionKey, CsnKeyEnd },
    { NSClearLineFunctionKey, CsnKeyClear },
    { NSExecuteFunctionKey, CsnKeyExecute },
    { NSHelpFunctionKey, CsnKeyHelp },
    { NSInsertFunctionKey, CsnKeyInsert },
    { NSMenuFunctionKey, CsnKeyApps },
    { NSPauseFunctionKey, CsnKeyPause },
    { NSPrintFunctionKey, CsnKeyPrint },
    { NSPrintScreenFunctionKey, CsnKeyPrintScreen },
    { NSScrollLockFunctionKey, CsnKeyScroll },
    { NSF1FunctionKey, CsnKeyF1 },
    { NSF2FunctionKey, CsnKeyF2 },
    { NSF3FunctionKey, CsnKeyF3 },
    { NSF4FunctionKey, CsnKeyF4 },
    { NSF5FunctionKey, CsnKeyF5 },
    { NSF6FunctionKey, CsnKeyF6 },
    { NSF7FunctionKey, CsnKeyF7 },
    { NSF8FunctionKey, CsnKeyF8 },
    { NSF9FunctionKey, CsnKeyF9 },
    { NSF10FunctionKey, CsnKeyF10 },
    { NSF11FunctionKey, CsnKeyF11 },
    { NSF12FunctionKey, CsnKeyF12 },
    { NSF13FunctionKey, CsnKeyF13 },
    { NSF14FunctionKey, CsnKeyF14 },
    { NSF15FunctionKey, CsnKeyF15 },
    { NSF16FunctionKey, CsnKeyF16 },
    { NSF17FunctionKey, CsnKeyF17 },
    { NSF18FunctionKey, CsnKeyF18 },
    { NSF19FunctionKey, CsnKeyF19 },
    { NSF20FunctionKey, CsnKeyF20 },
    { NSF21FunctionKey, CsnKeyF21 },
    { NSF22FunctionKey, CsnKeyF22 },
    { NSF23FunctionKey, CsnKeyF23 },
    { NSF24FunctionKey, CsnKeyF24 }
};

typedef std::array<CsnPhysicalKey, 0x7F> PhysicalKeyArray;

static PhysicalKeyArray BuildPhysicalKeyFromScanCode()
{
    PhysicalKeyArray result {};

    for (auto& keyInfo : keyInfos)
    {
        result[keyInfo.scanCode] = keyInfo.physicalKey;
    }

    return result;
}

PhysicalKeyArray physicalKeyFromScanCode = BuildPhysicalKeyFromScanCode();

static std::unordered_map<CsnPhysicalKey, CsnKey, std::hash<int>> BuildQwertyVirtualKeyFromPhysicalKey()
{
    std::unordered_map<CsnPhysicalKey, CsnKey, std::hash<int>> result;
    result.reserve(sizeof(keyInfos) / sizeof(keyInfos[0]));

    for (auto& keyInfo : keyInfos)
    {
        result[keyInfo.physicalKey] = keyInfo.qwertyKey;
    }

    return result;
}

std::unordered_map<CsnPhysicalKey, CsnKey, std::hash<int>> qwertyVirtualKeyFromPhysicalKey = BuildQwertyVirtualKeyFromPhysicalKey();

static std::unordered_map<CsnKey, uint16_t, std::hash<int>> BuildMenuCharFromVirtualKey()
{
    std::unordered_map<CsnKey, uint16_t, std::hash<int>> result;
    result.reserve(100);
    
    for (auto& keyInfo : keyInfos)
    {
        if (keyInfo.menuChar != 0)
            result[keyInfo.qwertyKey] = keyInfo.menuChar;
    }

    return result;
}

std::unordered_map<CsnKey, uint16_t, std::hash<int>> menuCharFromVirtualKey = BuildMenuCharFromVirtualKey();

static bool IsNumpadOrNumericKey(CsnPhysicalKey physicalKey)
{
    return (physicalKey >= CsnPhysicalKeyDigit0 && physicalKey <= CsnPhysicalKeyDigit9)
        || (physicalKey >= CsnPhysicalKeyNumLock && physicalKey <= CsnPhysicalKeyNumPadSubtract);
}

CsnPhysicalKey PhysicalKeyFromScanCode(uint16_t scanCode)
{
    return scanCode < physicalKeyFromScanCode.size() ? physicalKeyFromScanCode[scanCode] : CsnPhysicalKeyNone;
}

static bool IsAllowedAsciiChar(UniChar c)
{
    if (c < 0x20)
    {
        switch (c)
        {
            case kBackspaceCharCode:
            case kReturnCharCode:
            case kTabCharCode:
            case kEscapeCharCode:
                return true;
            default:
                return false;
        }
    }

    if (c == kDeleteCharCode)
        return false;

    return true;
}

static UniCharCount CharsFromScanCode(UInt16 scanCode, NSEventModifierFlags modifierFlags, UInt16 keyAction, UniChar* buffer, UniCharCount bufferSize)
{
    auto currentKeyboard = TISCopyCurrentKeyboardInputSource();
    if (!currentKeyboard)
        return 0;

    auto layoutData = static_cast<CFDataRef>(TISGetInputSourceProperty(currentKeyboard, kTISPropertyUnicodeKeyLayoutData));
    if (!layoutData)
        return 0;

    auto* keyboardLayout = reinterpret_cast<const UCKeyboardLayout*>(CFDataGetBytePtr(layoutData));

    UInt32 deadKeyState = 0;
    UniCharCount length = 0;
    
    int glyphModifiers = 0;
    if (modifierFlags & NSEventModifierFlagShift)
        glyphModifiers |= shiftKey;
    if (modifierFlags & NSEventModifierFlagCapsLock)
        glyphModifiers |= alphaLock;
    if (modifierFlags & NSEventModifierFlagOption)
        glyphModifiers |= optionKey;

    auto result = UCKeyTranslate(
        keyboardLayout,
        scanCode,
        keyAction,
        (glyphModifiers >> 8) & 0xFF,
        LMGetKbdType(),
        kUCKeyTranslateNoDeadKeysBit,
        &deadKeyState,
        bufferSize,
        &length,
        buffer);

    if (result != noErr)
        return 0;

    if (deadKeyState)
    {
        // translate a space with dead key state to get the dead key itself
        result = UCKeyTranslate(
            keyboardLayout,
            kVK_Space,
            keyAction,
            0,
            LMGetKbdType(),
            kUCKeyTranslateNoDeadKeysBit,
            &deadKeyState,
            bufferSize,
            &length,
            buffer);

        if (result != noErr)
            return 0;
    }

    if (length == 1 && buffer[0] <= 0x7F && !IsAllowedAsciiChar(buffer[0]))
        return 0;

    return length;
}

CsnKey VirtualKeyFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags)
{
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);
    if (!IsNumpadOrNumericKey(physicalKey))
    {
        const UniCharCount charCount = 4;
        UniChar chars[charCount];
        auto length = CharsFromScanCode(scanCode, modifierFlags, kUCKeyActionDown, chars, charCount);
        if (length > 0)
        {
            auto it = virtualKeyFromChar.find(chars[0]);
            if (it != virtualKeyFromChar.end())
                return it->second;
        }
    }

    auto it = qwertyVirtualKeyFromPhysicalKey.find(physicalKey);
    return it == qwertyVirtualKeyFromPhysicalKey.end() ? CsnKeyNone : it->second;
}

NSString* KeySymbolFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags)
{
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);

    const UniCharCount charCount = 4;
    UniChar chars[charCount];
    auto length = CharsFromScanCode(scanCode, modifierFlags, kUCKeyActionDisplay, chars, charCount);
    if (length > 0)
        return [NSString stringWithCharacters:chars length:length];

    auto it = qwertyVirtualKeyFromPhysicalKey.find(physicalKey);
    if (it == qwertyVirtualKeyFromPhysicalKey.end())
        return nullptr;

    auto menuChar = MenuCharFromVirtualKey(it->second);
    return menuChar == 0 || menuChar > 0x7E ? nullptr : [NSString stringWithCharacters:&menuChar length:1];
}

uint16_t MenuCharFromVirtualKey(CsnKey key)
{
    auto it = menuCharFromVirtualKey.find(key);
    return it == menuCharFromVirtualKey.end() ? 0 : it->second;
}
