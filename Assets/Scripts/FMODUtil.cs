using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FMODUtil
{
    /// <summary>
    /// This method must be called from a component's Awake method. Start is too late.
    /// </summary>
    public static void SetParam(FMODUnity.StudioEventEmitter emitter, string name, float value)
    {
        if (emitter.Params.Length == 0)
        {
            emitter.Params = new FMODUnity.ParamRef[]{
                new FMODUnity.ParamRef{
                    Name = name,
                    Value =value,
                }
            };
            return;
        }

        foreach (var param in emitter.Params)
        {
            if (param.Name == name)
            {
                param.Value = value;
                return;
            }
        }

        var paramList = new List<FMODUnity.ParamRef>(emitter.Params);
        paramList.Add(new FMODUnity.ParamRef
        {
            Name = name,
            Value = value,
        });
        emitter.Params = paramList.ToArray();
    }
}
