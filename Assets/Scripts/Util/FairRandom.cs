using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates "fair" random numbers across a distribution of slots, where "fair" means that
/// numbers that aren't generated are more likely to be generated later on, and vice versa.
/// 
/// This is useful for randomly spawning items or enemies in a way that feels natural to the
/// player. A "decay" configuration is provided that you can experiment with. A decay of 0
/// results in generating standard random numbers. A decay of 1 results in never generating the
/// same number twice in a row, but standard random numbers other than that.
/// </summary>
public class FairRandom
{
    private float[] weights;
    private float decay;

    public FairRandom(int initialSlots, float decay = 0.5f)
    {
        weights = new float[initialSlots];
        for (int i = 0; i < initialSlots; i++)
        {
            weights[i] = 1f / (float)initialSlots;
        }
        this.decay = decay;
    }

    /// <summary>
    /// Generates a random integer. This integer is less likely to be generated in the future.
    /// </summary>
    public int Int()
    {
        int value = weights.Length - 1;
        float r = Random.value;
        float sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += weights[i];
            if (r < sum)
            {
                value = i;
                break;
            }
        }
        if (weights.Length > 1)
        {
            float adjust = decay * weights[value];
            weights[value] -= adjust;
            for (int i = 0; i < weights.Length; i++)
            {
                if (i != value)
                {
                    weights[i] += adjust / (weights.Length - 1);
                }
            }
        }
        return value;
    }

    /// <summary>
    /// Adds an additional slot to the fair random and maintains the distribution of existing
    /// random numbers.
    /// </summary>
    public void AddSlot()
    {
        float[] weights = new float[this.weights.Length + 1];
        float value = 1f / (float)weights.Length;
        weights[weights.Length - 1] = value;
        for (int i = 0; i < weights.Length - 1; i++)
        {
            weights[i] = this.weights[i] - value / ((float)weights.Length - 1f);
        }
        this.weights = weights;
    }

    override public string ToString()
    {
        string weightsString = "[";
        foreach (var w in weights)
        {
            if (weightsString != "[")
            {
                weightsString += ",";
            }
            weightsString += w;
        }
        return weightsString + "]";
    }
}