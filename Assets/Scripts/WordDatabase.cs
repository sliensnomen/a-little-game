using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WordDatabase", menuName = "TrueName/WordDatabase")]
public class WordDatabase : ScriptableObject
{
    public List<string> holyWords = new List<string>();
    public List<string> darkWords = new List<string>();
    public List<string> dualWords = new List<string>();
}
