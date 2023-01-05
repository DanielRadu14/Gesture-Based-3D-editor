using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModePicker : UnityEngine.MonoBehaviour
{
    public enum GameMode { Default, Vertex, TBD };
    public GameMode gameModeStat = GameMode.Default;
}
