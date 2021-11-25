#if UNITY_WINDOWS

#include <Unity/Runtime.h>

#include <vector>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <windef.h>
#include <winuser.h>
#include <winbase.h>
#include <Stringapiset.h>

#define ID_EDITTEXT 200
#define EDITFIELD_HEIGHT 100
#define EDITFIELD_MARGINX 10
#define EDITFIELD_MARGINY 20

#define WM_HIDEEDITFIELD (WM_USER + 1)

static HWND hWndMain = NULL;
static HWND hWndDlg = NULL;
static HWND hWndEdit = NULL;
static int editFieldType = 0;
static bool editFieldHidden = FALSE;
static bool editFieldMultiline = FALSE;
static bool editFieldSecure = FALSE;
static WORD editFieldX, editFieldY, editFieldCX, editFieldCY;
static std::vector<WCHAR> inputString;
static int editFieldCharacterLimit;
static DWORD editFieldSelectionStart, editFieldSelectionEnd;
static bool inputActive;
static bool inputCanceled;
static std::mutex softInputLock;

DOTS_EXPORT(void) HideSoftInput();
DOTS_EXPORT(void) SetCharacterLimit(int limit);
DOTS_EXPORT(void) SetHideInputField(bool hidden);
DOTS_EXPORT(void) SetInputSelection(int start, int length);
LRESULT OpenEditField(HWND hwndOwner);

class EditFieldThreadHelper
{
    std::thread* m_Thread;
    std::mutex m_StartMutex;
    std::condition_variable m_StartCondition;
    bool m_DialogStarted;

    void Start()
    {
        OpenEditField(hWndMain);
    }

    void StopAndDelete()
    {
        if (m_Thread != NULL)
        {
            HideSoftInput();
            m_Thread->join();
            delete m_Thread;
        }
        m_DialogStarted = false;
    }

public:
    EditFieldThreadHelper() : m_Thread(NULL), m_DialogStarted(false) {}
    ~EditFieldThreadHelper()
    {
        StopAndDelete();
    }
    EditFieldThreadHelper(const EditFieldThreadHelper&) = delete;
    EditFieldThreadHelper& operator=(const EditFieldThreadHelper&) = delete;

    void StartDialog()
    {
        StopAndDelete();
        m_Thread = new std::thread(&EditFieldThreadHelper::Start, this);
        std::unique_lock<std::mutex> lock(m_StartMutex);
        m_StartCondition.wait(lock, [this] { return m_DialogStarted; });
    }

    void DialogStarted()
    {
        {
            std::lock_guard<std::mutex> lock(m_StartMutex);
            m_DialogStarted = true;
        }
        m_StartCondition.notify_one();
    }

};
static EditFieldThreadHelper editFieldThread;

LPWORD lpwAlign(LPWORD lpIn)
{
    ULONG64 ul;
    ul = (ULONG64)lpIn;
    ul ++;
    ul >>=1;
    ul <<=1;
    return (LPWORD)ul;
}

void UpdateInputString()
{
    if (hWndEdit != NULL)
    {
        std::lock_guard<std::mutex> lock(softInputLock);
        // extra character is required to get final 0 char
        int length = GetWindowTextLength(hWndEdit) + 1;
        inputString.resize(length);
        GetWindowText(hWndEdit, inputString.data(), length);
    }
}

void HideInputField(bool hidden)
{
    if (hWndEdit == NULL)
    {
        return;
    }
    if (hidden)
    {
        MoveWindow(hWndEdit, 0, 0, 0, 0, TRUE);
    }
    else
    {
        MoveWindow(hWndEdit, editFieldX, editFieldY, editFieldCX, editFieldCY, TRUE);
    }
    HWND hwnd = SetFocus(hWndEdit);
}

BOOL CALLBACK DialogProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam) 
{
    switch (message) 
    {
        case WM_INITDIALOG:
            hWndDlg = hwnd;
            hWndEdit = GetDlgItem(hwnd, ID_EDITTEXT);
            SetCharacterLimit(editFieldCharacterLimit);
            SetInputSelection(editFieldSelectionStart, editFieldSelectionEnd - editFieldSelectionStart);
            SetHideInputField(editFieldHidden);
            editFieldThread.DialogStarted();
            return FALSE;

        case WM_COMMAND:
            switch (LOWORD(wParam))
            { 
                case IDOK:
                    UpdateInputString();

                case IDCANCEL:
                {
                    std::lock_guard<std::mutex> lock(softInputLock);
                    inputActive = FALSE;
                    inputCanceled = LOWORD(wParam) == IDCANCEL;
                    hWndEdit = NULL;
                    hWndDlg = NULL;
                    EndDialog(hwnd, wParam);
                    return TRUE;
                }
            }
            return FALSE;

        case WM_CTLCOLOREDIT:
            if ((HWND)lParam == hWndEdit)
            {
                std::lock_guard<std::mutex> lock(softInputLock);
                SendMessage(hWndEdit, EM_GETSEL, (WPARAM)&editFieldSelectionStart, (LPARAM)&editFieldSelectionEnd);
            }
            return FALSE;

        case WM_MOUSEACTIVATE:
            if (hWndEdit != NULL)
            {
                std::lock_guard<std::mutex> lock(softInputLock);
                SendMessage(hWndEdit, EM_GETSEL, (WPARAM)&editFieldSelectionStart, (LPARAM)&editFieldSelectionEnd);
            }
            return FALSE;

        case WM_HIDEEDITFIELD:
            HideInputField((BOOL)wParam);
            return TRUE;
    }

    return FALSE;
}

LRESULT OpenEditField(HWND hwndOwner)
{
    HGLOBAL hgbl;
    LPDLGTEMPLATE lpdt;
    LPDLGITEMTEMPLATE lpdit;
    LPWORD lpw;
    LPWSTR lpwsz;
    LRESULT ret;
    int nchar;

    hgbl = GlobalAlloc(GMEM_ZEROINIT, 2048);
    if (!hgbl)
    {
        return -1;
    }

    lpdt = (LPDLGTEMPLATE)GlobalLock(hgbl);

    // Define a dialog box.
    lpdt->style = WS_POPUP | WS_BORDER | WS_SYSMENU | DS_MODALFRAME | WS_CAPTION;
    lpdt->cdit = 3;         // Number of controls

    RECT mainWindowRect;
    GetClientRect(hwndOwner, &mainWindowRect);
    LONG units = GetDialogBaseUnits();
    WORD baseunitX = LOWORD(units);
    WORD baseunitY = HIWORD(units);
    WORD mainWindowWidth = (WORD)MulDiv(mainWindowRect.right, 4, baseunitX);
    WORD mainWindowHeight = (WORD)MulDiv(mainWindowRect.bottom, 8, baseunitY);
    WORD editFieldWidth = mainWindowWidth - EDITFIELD_MARGINX * 2;
    WORD editFieldHeight = EDITFIELD_HEIGHT;

    // Dialog size and position
    lpdt->x = EDITFIELD_MARGINX;
    lpdt->y = mainWindowHeight - EDITFIELD_HEIGHT - EDITFIELD_MARGINY;
    lpdt->cx = editFieldWidth;
    lpdt->cy = editFieldHeight;

    lpw = (LPWORD)(lpdt + 1);
    *lpw++ = 0;             // No menu
    *lpw++ = 0;             // Predefined dialog box class (by default)

    lpwsz = (LPWSTR)lpw;
    nchar = 1 + MultiByteToWideChar(CP_ACP, 0, "Edit text", -1, lpwsz, 50);
    lpw += nchar;

    //-----------------------
    // Define an OK button.
    //-----------------------
    lpw = lpwAlign(lpw);    // Align DLGITEMTEMPLATE on DWORD boundary
    lpdit = (LPDLGITEMTEMPLATE)lpw;
    lpdit->x = editFieldWidth / 16; lpdit->y = 3 * editFieldHeight / 4;
    lpdit->cx = editFieldWidth / 4; lpdit->cy = editFieldHeight / 6;
    lpdit->id = IDOK;       // OK button identifier
    lpdit->style = WS_CHILD | WS_VISIBLE | BS_DEFPUSHBUTTON;

    lpw = (LPWORD)(lpdit + 1);
    *lpw++ = 0xFFFF;
    *lpw++ = 0x0080;        // Button class

    lpwsz = (LPWSTR)lpw;
    nchar = 1 + MultiByteToWideChar(CP_ACP, 0, "OK", -1, lpwsz, 50);
    lpw += nchar;
    *lpw++ = 0;             // No creation data

    //-----------------------
    // Define a Cancel button.
    //-----------------------
    lpw = lpwAlign(lpw);    // Align DLGITEMTEMPLATE on DWORD boundary
    lpdit = (LPDLGITEMTEMPLATE)lpw;
    lpdit->x = 11 * editFieldWidth / 16; lpdit->y = 3 * editFieldHeight / 4;
    lpdit->cx = editFieldWidth / 4; lpdit->cy = editFieldHeight / 6;
    lpdit->id = IDCANCEL;    // Cancel button identifier
    lpdit->style = WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON;

    lpw = (LPWORD)(lpdit + 1);
    *lpw++ = 0xFFFF;
    *lpw++ = 0x0080;        // Button class atom

    lpwsz = (LPWSTR)lpw;
    nchar = 1 + MultiByteToWideChar(CP_ACP, 0, "Cancel", -1, lpwsz, 50);
    lpw += nchar;
    *lpw++ = 0;             // No creation data

    //-----------------------
    // Define a edit text control.
    //-----------------------
    lpw = lpwAlign(lpw);    // Align DLGITEMTEMPLATE on DWORD boundary
    lpdit = (LPDLGITEMTEMPLATE)lpw;
    lpdit->x = editFieldWidth / 16; lpdit->y = editFieldHeight / 8;
    lpdit->cx = 7 * editFieldWidth / 8; lpdit->cy = 3 * editFieldHeight / 8;
    lpdit->id = ID_EDITTEXT;    // EditText identifier
    lpdit->style = WS_CHILD | WS_VISIBLE | ES_LEFT;
    if (editFieldMultiline)
    {
        lpdit->style |= ES_MULTILINE | ES_WANTRETURN | ES_AUTOVSCROLL;
    }
    if (editFieldSecure)
    {
        lpdit->style |= ES_PASSWORD;
    }
    if (editFieldType == 4)
    {
        lpdit->style |= ES_NUMBER;
    }

    editFieldX = MulDiv(lpdit->x, baseunitX, 4);
    editFieldCX = MulDiv(lpdit->cx, baseunitX, 4);
    editFieldY = MulDiv(lpdit->y, baseunitY, 8);
    editFieldCY = MulDiv(lpdit->cy, baseunitY, 8);

    lpw = (LPWORD)(lpdit + 1);
    *lpw++ = 0xFFFF;
    *lpw++ = 0x0081;        // Static class

    WCHAR *lpszMessage = inputString.data();
    for (lpwsz = (LPWSTR)lpw; *lpwsz++ = (WCHAR)*lpszMessage++;);
    lpw = (LPWORD)lpwsz;
    *lpw++ = 0;             // No creation data

    GlobalUnlock(hgbl);
    ret = DialogBoxIndirect(NULL, 
                           (LPDLGTEMPLATE)hgbl,
                           hwndOwner,
                           (DLGPROC)DialogProc);
    GlobalFree(hgbl);
    return ret; 
}

DOTS_EXPORT(void)
ShowSoftInput(const int* hwnd, const WCHAR* initialText, int length, int type,
              bool correction, bool multiline, bool secure,
              const WCHAR* placeholder, int placeholderLength, int characterLimit, bool isInputFieldHidden,
              int selectionStart, int selectionLength)
{
    std::lock_guard<std::mutex> lock(softInputLock);
    hWndMain = (HWND)hwnd;
    // this implementation ignores correction, placeholder
    // also only type == 4 (Number Pad)affects edit field behavior
    editFieldType = type;
    inputString.resize(length + 1);
    memcpy(inputString.data(), initialText, length * sizeof(WCHAR));
    inputString[length] = 0;
    editFieldCharacterLimit = characterLimit;
    editFieldMultiline = multiline;
    editFieldSecure = secure;
    editFieldSelectionStart = selectionStart;
    editFieldSelectionEnd = editFieldSelectionStart + selectionLength;
    editFieldHidden = isInputFieldHidden;
    inputActive = TRUE;
    inputCanceled = FALSE;
    editFieldThread.StartDialog();
}

DOTS_EXPORT(void)
HideSoftInput()
{
    if (hWndDlg != NULL)
    {
        EndDialog(hWndDlg, 0);
        hWndDlg = NULL;
        hWndEdit = NULL;
        std::lock_guard<std::mutex> lock(softInputLock);
        inputActive = FALSE;
        inputCanceled = TRUE;
    }
}

DOTS_EXPORT(void)
SetSoftInputString(const WCHAR* text, int length)
{
    {
        std::lock_guard<std::mutex> lock(softInputLock);
        inputString.resize(length + 1);
        memcpy(inputString.data(), text, length * sizeof(WCHAR));
        inputString[length] = 0;
    }
    if (hWndEdit != NULL)
    {
        SetWindowText(hWndEdit, inputString.data());
    }
}

DOTS_EXPORT(const WCHAR*)
GetSoftInputString(int* len, bool* updated)
{
    if (len == NULL || updated == NULL)
    {
        return NULL;
    }

    *updated = false;
    if (hWndEdit != NULL)
    {
        // extra character is required to get final 0 char
        int length = GetWindowTextLength(hWndEdit) + 1;
        std::vector<WCHAR> newInputString(length);
        GetWindowText(hWndEdit, newInputString.data(), length);
        if (length != (int)inputString.size() || memcmp(newInputString.data(), inputString.data(), length * sizeof(WCHAR)))
        {
            std::lock_guard<std::mutex> lock(softInputLock);
            *updated = true;
            inputString.resize(length);
            memcpy(inputString.data(), newInputString.data(), length * sizeof(WCHAR));
        }
    }
    *len = (int)inputString.size() - 1;
    return inputString.data();
}

DOTS_EXPORT(void)
SetInputSelection(int start, int length)
{
    if (hWndEdit != NULL)
    {
        SendMessage(hWndEdit, EM_SETSEL, start, start + length);
    }
}

DOTS_EXPORT(bool)
GetInputSelection(int* start, int* length)
{
    if (start == NULL || length == NULL)
    {
        return FALSE;
    }

    std::lock_guard<std::mutex> lock(softInputLock);
    *start = (int)editFieldSelectionStart;
    *length = (int)(editFieldSelectionEnd - editFieldSelectionStart);
    return TRUE;
}

DOTS_EXPORT(void)
SetCharacterLimit(int limit)
{
    if (hWndEdit != NULL)
    {
        SendMessage(hWndEdit, EM_SETLIMITTEXT, limit, 0);
    }
}

DOTS_EXPORT(void)
SetHideInputField(bool hidden)
{
    if (hWndDlg != NULL)
    {
        SendMessage(hWndDlg, WM_HIDEEDITFIELD, hidden, 0);
    }
}

DOTS_EXPORT(bool)
GetInputArea(bool* visible, int* x, int* y, int* width, int* height)
{
    if (visible == NULL || x == NULL || y == NULL || width == NULL || height == NULL)
        return FALSE;

    *visible = hWndDlg != NULL;
    if (*visible)
    {
        RECT mainWindowRect;
        GetClientRect(hWndMain, &mainWindowRect);
        RECT editDlgRect;
        GetWindowRect(hWndDlg, &editDlgRect);
        POINT origin;
        origin.x = origin.y = 0;
        ClientToScreen(hWndMain, &origin);
        *x = editDlgRect.left - origin.x;
        *width = editDlgRect.right - editDlgRect.left;
        *y = mainWindowRect.bottom - (editDlgRect.bottom - origin.y);
        *height = editDlgRect.bottom - editDlgRect.top;
    }
    return TRUE;
}

DOTS_EXPORT(void)
GetSoftInputState(bool* active, bool* canceled)
{
    std::lock_guard<std::mutex> lock(softInputLock);
    if (active == NULL || canceled == NULL)
    {
        return;
    }
    *active = inputActive;
    *canceled = inputCanceled;
}

#endif
