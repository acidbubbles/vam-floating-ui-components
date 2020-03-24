using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VaM Utilities
/// By Acidbubbles
/// Control for sliding between values
/// Source: https://github.com/acidbubbles/vam-utilities
/// </summary>
public class UISlider : MVRScript
{
    private JSONStorableString _labelJSON;
    private JSONStorableFloat _valueJSON;
    private JSONStorableStringChooser _atomJSON;
    private JSONStorableStringChooser _storableJSON;
    private JSONStorableStringChooser _storableParamJSON;
    private Atom _atom;
    private JSONStorableFloat _targetParam;
    private Transform _uiTransform;
    private UIDynamicSlider _ui;

    public override void Init()
    {
        try
        {
            _atom = GetAtom();

            _labelJSON = new JSONStorableString("Label", "My Slider", label =>
            {
                if (_ui == null) return;
                _ui.label = label;
            });
            RegisterString(_labelJSON);
            CreateTextInput(_labelJSON);

            _valueJSON = new JSONStorableFloat(_labelJSON.val, 0f, v => UpdateValue(v), 0f, 10f, false);
            RegisterFloat(_valueJSON);

            OnEnable();

            _atomJSON = new JSONStorableStringChooser("Target atom", SuperController.singleton.GetAtomUIDs(), "", "Target atom", uid => OnTargetAtomChanged(uid));
            RegisterStringChooser(_atomJSON);
            var atomPopup = CreateScrollablePopup(_atomJSON, true);
            atomPopup.popupPanelHeight = 800f;
            SuperController.singleton.onAtomUIDsChangedHandlers += (uids) => OnAtomsChanged(uids);
            OnAtomsChanged(SuperController.singleton.GetAtomUIDs());

            var storables = new List<string>(new[] { "" });
            _storableJSON = new JSONStorableStringChooser("Target storable", storables, "", "Target storable", storable => OnTargetStorableChanged(storable));
            RegisterStringChooser(_storableJSON);
            var storablePopup = CreateScrollablePopup(_storableJSON, true);
            storablePopup.popupPanelHeight = 800f;

            var storableParams = new List<string>(new[] { "" });
            _storableParamJSON = new JSONStorableStringChooser("Target param", storables, "", "Target param", floatParam => OnTargetParamChanged(floatParam));
            RegisterStringChooser(_storableParamJSON);
            var storableParamUI = CreateScrollablePopup(_storableParamJSON, true);
            storableParamUI.popupPanelHeight = 800f;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(Init)}: " + exc);
        }
    }

    private Atom GetAtom()
    {
        // Note: Yeah, that's horrible, but containingAtom is null
        var container = gameObject?.transform?.parent?.parent?.parent?.parent?.parent?.gameObject;
        if (container == null) throw new NullReferenceException($"{nameof(UISlider)} could not find the parent gameObject");
        var atom = container.GetComponent<Atom>();
        if (atom == null) throw new NullReferenceException($"{nameof(UISlider)} could not find the parent atom in {container.name}");
        if (atom.type != "SimpleSign") throw new InvalidOperationException($"{nameof(UISlider)} can only be applied on SimpleSign");
        return atom;
    }

    private void OnAtomsChanged(List<string> uids)
    {
        try
        {
            var atoms = new List<string>(uids);
            atoms.Insert(0, "");
            _atomJSON.choices = atoms;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(OnAtomsChanged)}: " + exc);
        }
    }

    private void OnTargetAtomChanged(string uid)
    {
        try
        {
            if (uid == "")
            {
                _storableJSON.choices = new List<string>(new[] { "" });
                _storableJSON.val = "";
                return;
            }

            var atom = SuperController.singleton.GetAtomByUid(uid);
            if (atom == null) throw new NullReferenceException($"Atom {uid} does not exist");
            var storables = new List<string>(atom.GetStorableIDs());
            storables.Insert(0, "");
            _storableJSON.choices = storables;
            _storableJSON.val = storables.FirstOrDefault(s => s == _storableJSON.val) ?? "";
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(OnTargetAtomChanged)}: " + exc);
        }
    }

    private void OnTargetStorableChanged(string sid)
    {
        try
        {
            if (sid == "")
            {
                _storableParamJSON.choices = new List<string>(new[] { "" });
                _storableParamJSON.val = "";
                return;
            }

            if (_atomJSON.val == "") return;

            var atom = SuperController.singleton.GetAtomByUid(_atomJSON.val);
            if (atom == null) throw new NullReferenceException($"Atom {_atomJSON.val} does not exist");
            var storable = atom.GetStorableByID(sid);
            if (storable == null) throw new NullReferenceException($"Storable {sid} of atom {_atomJSON.val} does not exist");
            var floatParams = new List<string>(storable.GetFloatParamNames());
            floatParams.Insert(0, "");
            _storableParamJSON.choices = floatParams;
            _storableParamJSON.val = floatParamNames.FirstOrDefault(s => s == _storableParamJSON.val) ?? "";
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(OnTargetStorableChanged)}: " + exc);
        }
    }

    private void OnTargetParamChanged(string paramName)
    {

        if (_atomJSON.val == "") return;
        if (_storableJSON.val == "") return;
        if (paramName == "") return;

        var atom = SuperController.singleton.GetAtomByUid(_atomJSON.val);
        if (atom == null) throw new NullReferenceException($"Atom {_atomJSON.val} does not exist");
        var storable = atom.GetStorableByID(_storableJSON.val);
        if (storable == null) throw new NullReferenceException($"Storable {_storableJSON.val} of atom {_atomJSON.val} does not exist");
        _targetParam = storable.GetFloatJSONParam(paramName);
        if (_targetParam == null) throw new NullReferenceException($"Float JSON param {paramName} of storable {_storableJSON.val} of atom {_atomJSON.val} does not exist");

        _valueJSON.constrained = false;
        _valueJSON.defaultVal = _targetParam.defaultVal;
        _valueJSON.val = _targetParam.val;
        _valueJSON.min = _targetParam.min;
        _valueJSON.max = _targetParam.max;
        _valueJSON.constrained = _targetParam.constrained;
        _ui.Configure(_labelJSON.val, _valueJSON.min, _valueJSON.max, _valueJSON.defaultVal, _valueJSON.constrained, "F2", true, !_valueJSON.constrained);
    }

    private void UpdateValue(float v)
    {
        if (_targetParam == null) return;
        _targetParam.val = v;
    }

    public void OnEnable()
    {
        if (_uiTransform != null || _atom == null) return;

        try
        {
            var canvas = _atom.GetComponentInChildren<Canvas>();
            if (canvas == null) throw new NullReferenceException("Could not find a canvas to attach to");

            _uiTransform = Instantiate(manager.configurableSliderPrefab.transform);
            if (_uiTransform == null) throw new NullReferenceException("Could not instantiate configurableSliderPrefab");
            _uiTransform.SetParent(canvas.transform, false);
            _uiTransform.gameObject.SetActive(true);

            _ui = _uiTransform.GetComponent<UIDynamicSlider>();
            if (_ui == null) throw new NullReferenceException("Could not find a UIDynamicSlider component");
            _ui.Configure(_labelJSON.val, _valueJSON.min, _valueJSON.max, _valueJSON.val, _valueJSON.constrained, "F2", true, !_valueJSON.constrained);
            _valueJSON.slider = _ui.slider;

            _uiTransform.Translate(Vector3.down * 0.3f, Space.Self);
            _uiTransform.Translate(Vector3.right * 0.35f, Space.Self);
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(OnEnable)}: " + exc);
        }
    }

    public void OnDisable()
    {
        if (_uiTransform == null) return;

        try
        {
            _valueJSON.slider = null;
            Destroy(_uiTransform.gameObject);
            _uiTransform = null;
            _ui = null;
        }
        catch (Exception exc)
        {
            SuperController.LogError($"{nameof(UISlider)}.{nameof(OnDisable)}: " + exc);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }

    private void CreateTextInput(JSONStorableString jss, bool rightSide = false)
    {
        var textfield = CreateTextField(jss, rightSide);
        textfield.height = 1f;
        textfield.backgroundColor = Color.white;
        var input = textfield.gameObject.AddComponent<InputField>();
        input.textComponent = textfield.UItext;
        jss.inputField = input;
    }
}
