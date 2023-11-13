using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class TestUtils
    {
        static public IEnumerator WaitUntil(Func<bool> condition, float timeout = 10.0f)
        {
            float timePassed = 0f;
            while (!condition() && timePassed < timeout) {
                yield return new WaitForEndOfFrame();
                timePassed += Time.deltaTime;
            }
            if(timePassed >= timeout) {
                throw new TimeoutException("Condition was not fulfilled for " + timeout + " seconds.");
            }
        }
    }
}