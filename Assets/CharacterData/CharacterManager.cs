using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    private readonly List<string> warriorAttacks = new()
    {
        "Stab",
        "Slash"
    };
    private readonly List<string> archerAttacks = new()
    {
        "Shoot",
        "Snipe"
    };
    private readonly List<string> priestAttacks = new()
    {
        "Basic Heal",
        "Blessing of Regeneration"
    };
    private readonly float[] maxHealths = { 100, 80, 50 };
    private readonly float[] currentHealths = { 100, 80, 50 };
    private readonly float[] armors = {5, 3, 2 };
    private readonly float[] actionSpeeds = {10, 12, 8 };
    private readonly float[] physicalSpeeds = {10, 12, 12};
    private float currentExperience = 0;
    private int level = 1;

    public void ApplyExperience(float experience)
    {
        currentExperience += experience;
        if(currentExperience >= 250 * Mathf.Pow(2, level))
        {
            currentExperience -= 250 * Mathf.Pow(2, level);
            level++;
            maxHealths[0] += 10;
            maxHealths[1] += 8;
            maxHealths[2] += 5;
            currentHealths[0] += currentHealths[0] > 0 ? 10 : 0;
            currentHealths[1] += currentHealths[1] > 0 ? 8 : 0;
            currentHealths[2] += currentHealths[2] > 0 ? 5 : 0;
            armors[0] += 0.5f;
            armors[1] += 0.3f;
            armors[2] += 0.2f;
            actionSpeeds[0]++;
            actionSpeeds[1] += 1.2f;
            actionSpeeds[2] += 0.8f;
            physicalSpeeds[0]++;
            physicalSpeeds[1] += 1.2f;
            physicalSpeeds[2] += 1.2f;
        }
    }

    public float GetArmor(int character)
    {
        if (character < 0 || character >= armors.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        return armors[character];
    }

    public float GetMaxHealth(int character)
    {
        if (character < 0 || character >= maxHealths.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        return maxHealths[character];
    }

    public float GetCurrentHealth(int character)
    {
        if (character < 0 || character >= currentHealths.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        return currentHealths[character];
    }

    public float GetActionSpeed(int character)
    {
        if (character < 0 || character >= actionSpeeds.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        return actionSpeeds[character];
    }

    public float GetPhysicalSpeed(int character)
    {
        if (character < 0 || character >= physicalSpeeds.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        return physicalSpeeds[character];
    }

    public void ChangeHealth(int character, float change)
    {
        if (character < 0 || character >= currentHealths.Length)
        {
            throw new ArgumentException("Character must exist");
        }
        currentHealths[character] += change;
        currentHealths[character] = Mathf.Clamp(currentHealths[character], 0, maxHealths[character]);
    }

    public List<string> GetActions(int character)
    {
        switch(character)
        {
            case 0:
                {
                    return warriorAttacks;
                }
            case 1:
                {
                    return archerAttacks;
                }
            case 2:
                {
                    return priestAttacks;
                }
            default:
                {
                    throw new ArgumentException("Character must exist");
                }
        }
    }

    public int GetLevel()
    {
        return level;
    }

    public void Load()
    {
        int loadedLevel = PlayerPrefs.GetInt("PartyLevel");
        while(level < loadedLevel)
        {
            ApplyExperience(250 * Mathf.Pow(2, level));
        }
        currentExperience = PlayerPrefs.GetFloat("CurrentExperience");
        currentHealths[0] = PlayerPrefs.GetFloat("WarriorCurrentHealth");
        currentHealths[1] = PlayerPrefs.GetFloat("ArcherCurrentHealth");
        currentHealths[2] = PlayerPrefs.GetFloat("PriestCurrentHealth");
    }

    public void Save()
    {
        PlayerPrefs.SetInt("PartyLevel", level);
        PlayerPrefs.SetFloat("CurrentExperience", currentExperience);
        PlayerPrefs.SetFloat("WarriorCurrentHealth", currentHealths[0]);
        PlayerPrefs.SetFloat("ArcherCurrentHealth", currentHealths[1]);
        PlayerPrefs.SetFloat("PriestCurrentHealth", currentHealths[2]);
        PlayerPrefs.Save();
    }
}
