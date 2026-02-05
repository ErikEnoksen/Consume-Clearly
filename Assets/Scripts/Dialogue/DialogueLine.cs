using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    [TextArea] public string text;
    public string speakerName = "";
    public Color dialogueBoxColor = Color.white;
    public Color textColor = Color.black;
    public Sprite dialogueBoxSprite;
}
