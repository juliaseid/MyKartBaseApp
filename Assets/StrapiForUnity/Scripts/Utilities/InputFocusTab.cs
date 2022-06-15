using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputFocusTab : MonoBehaviour
{
    private InputField[] _inputFields;
    private int _fieldIndex;
    
    // Start is called before the first frame update
    void Start()
    {
        _inputFields = GetComponentsInChildren<InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (_inputFields.Length <= _fieldIndex)
            {
                _fieldIndex = 0;
            }

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                InputField currentlySelectedInputField =
                    EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
                if (currentlySelectedInputField != null)
                {
                    for (int i = 0; i < _inputFields.Length; i++)
                    {
                        if (_inputFields[i] == currentlySelectedInputField)
                        {
                            _fieldIndex = i+1;
                        }
                    }
                }
            }
            while (!_inputFields[_fieldIndex].isActiveAndEnabled && (_fieldIndex <= _inputFields.Length))
            {
                _fieldIndex++;
            }

            if (_fieldIndex >= _inputFields.Length)
            {
                return;
            }
            _inputFields[_fieldIndex].Select();
            _fieldIndex++;
        }
    }
}
