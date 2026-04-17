using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TMPTypeWriter
{
    #region Field

    private float _typingSpeed;
    private float _punctuationDelay;
    private bool _enablePunctuationDelay;

    private Coroutine _typingCoroutine;
    private string _currentText;
    private bool _isTyping;
    private bool _skipTyping;

    private readonly TMPro.TMP_Text _textPro;

    private Action _onTypingComplete;

    #endregion

    #region Interface

    public TMPTypeWriter(TMPro.TMP_Text textPro, float typingSpeed = 0.05f,
        float punctuationDelay = 0.3f, bool enablePunctuationDelay = false)
    {
        _textPro = textPro;
        _typingSpeed = typingSpeed;
        _punctuationDelay = punctuationDelay;
        _enablePunctuationDelay = enablePunctuationDelay;
    }

    public void SetTypeWriter(float typingSpeed = 0.05f, float punctuationDelay = 0.3f,
        bool enablePunctuationDelay = false)
    {
        _typingSpeed = typingSpeed;
        _punctuationDelay = punctuationDelay;
        _enablePunctuationDelay = enablePunctuationDelay;
    }

    public void SetCompleteFunc(Action onTypingComplete)
    {
        _onTypingComplete = onTypingComplete;
    }

    public void StartTyping(string text)
    {
        if (_typingCoroutine != null)
        {
            _textPro.StopCoroutine(_typingCoroutine);
        }

        _currentText = text;
        _skipTyping = false;
        _typingCoroutine = _textPro.StartCoroutine(TypeText());
    }

    public void Skip()
    {
        if (_isTyping)
        {
            _skipTyping = true;
        }
    }

    public void Stop()
    {
        if (_typingCoroutine != null)
        {
            _textPro.StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _isTyping = false;
    }

    public bool IsTyping()
    {
        return _isTyping;
    }

    public void OnDestroy()
    {
        Stop();
        _onTypingComplete = null;
    }

    #endregion

    #region Method

    private IEnumerator TypeText()
    {
        _isTyping = true;
        _textPro.text = _currentText;
        _textPro.ForceMeshUpdate();
        _textPro.maxVisibleCharacters = 0;
        var totalCharacters = _textPro.textInfo.characterCount;
        for (var i = 0; i <= totalCharacters; i++)
        {
            if (_skipTyping)
            {
                _textPro.maxVisibleCharacters = totalCharacters;
                break;
            }

            _textPro.maxVisibleCharacters = i;

            PlayTypingSound();

            if (i < totalCharacters)
            {
                var currentChar = _textPro.textInfo.characterInfo[i].character;
                var delay = GetCharacterDelay(currentChar);
                yield return new WaitForSeconds(delay);
            }
        }

        _skipTyping = false;
        _isTyping = false;
        _typingCoroutine = null;

        _onTypingComplete?.Invoke();
    }

    private float GetCharacterDelay(char c)
    {
        if (!_enablePunctuationDelay) return _typingSpeed;
        switch (c)
        {
            case '.':
            case '。':
            case '!':
            case '！':
            case '?':
            case '？':
                return _typingSpeed + _punctuationDelay;
            case ',':
            case '，':
            case ';':
            case '；':
                return _typingSpeed + _punctuationDelay * 0.5f;
            case ' ':
                return 0f;
        }

        return _typingSpeed;
    }

    private void PlayTypingSound()
    {
        // if (typingSound != null && audioSource != null)
        // {
        //     audioSource.PlayOneShot(typingSound);
        // }
    }

    #endregion
}