using System;
using System.Collections;
using UnityEngine;

public static class Utilities {
  public static IEnumerator WaitForSecondsAnd(float seconds, Action action) {
    yield return new WaitForSeconds(seconds);
    action();
  }
}
