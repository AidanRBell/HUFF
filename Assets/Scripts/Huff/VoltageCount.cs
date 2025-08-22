using UnityEngine;
using System;

public class VoltageCount : MonoBehaviour
{
    int voltageCount = 0;

    [SerializeField] GameObject[] voltIcons;

    void Start()
    {
        if (voltIcons.Length != 10)
            throw new Exception("error: volt icons game objects provided != 10");


    }


    public void increaseVoltageCount()
    {
        voltageCount++;

        if (voltageCount < 0) voltageCount = 0;
        else if (voltageCount > 10) voltageCount = 10;

        setVoltIcons();
    }

    public void decreaseVoltageCount()
    {
        voltageCount--;

        if (voltageCount < 0) voltageCount = 0;
        else if (voltageCount > 10) voltageCount = 10;

        setVoltIcons();
    }

    public void setVoltageCount(int v)
    {
        if (voltageCount == v) return; // neither the voltage count nor the on screen icons need to change

        voltageCount = v;

        if (voltageCount < 0) voltageCount = 0;
        else if (voltageCount > 10) voltageCount = 10;

        setVoltIcons();
    }

    public void showVoltageCount()
    {

    }

    public void hideVoltageCount()
    {

    }

    private void setVoltIcons()
    {

    }

}
