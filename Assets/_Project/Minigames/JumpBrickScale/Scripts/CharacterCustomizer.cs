using UnityEngine;

public class CharacterCustomizer : MonoBehaviour
{
    [Header("Mesh Renderers (The Child Objects)")]
    public MeshRenderer headRenderer;
    public MeshRenderer bodyRenderer;
    public MeshRenderer feetRenderer;

    [Header("Materials")]
    public Material[] headMaterials;
    public Material[] bodyMaterials;
    public Material[] feetMaterials;

    private int headIndex;
    private int bodyIndex;
    private int feetIndex;

    private void Start()
    {
        LoadCustomization();
    }

    public void ChangeHead(int direction)
    {
        headIndex = GetWrappedIndex(headIndex, direction, headMaterials.Length);
        ApplyAndSave(headRenderer, headMaterials[headIndex], "SavedHead", headIndex);
    }

    public void ChangeBody(int direction)
    {
        bodyIndex = GetWrappedIndex(bodyIndex, direction, bodyMaterials.Length);
        ApplyAndSave(bodyRenderer, bodyMaterials[bodyIndex], "SavedBody", bodyIndex);
    }

    public void ChangeFeet(int direction)
    {
        feetIndex = GetWrappedIndex(feetIndex, direction, feetMaterials.Length);
        ApplyAndSave(feetRenderer, feetMaterials[feetIndex], "SavedFeet", feetIndex);
    }

    private int GetWrappedIndex(int currentIndex, int direction, int arrayLength)
    {
        if (arrayLength == 0) return 0;
        
        int newIndex = currentIndex + direction;
        if (newIndex < 0) return arrayLength - 1;
        if (newIndex >= arrayLength) return 0;
        
        return newIndex;
    }

    private void ApplyAndSave(MeshRenderer renderer, Material mat, string prefKey, int index)
    {
        if (renderer != null && mat != null)
        {
            renderer.material = mat;
            PlayerPrefs.SetInt(prefKey, index);
            PlayerPrefs.Save();
        }
    }

    private void LoadCustomization()
    {
        headIndex = PlayerPrefs.GetInt("SavedHead", 0);
        bodyIndex = PlayerPrefs.GetInt("SavedBody", 0);
        feetIndex = PlayerPrefs.GetInt("SavedFeet", 0);

        if (headMaterials.Length > 0 && headRenderer != null) 
            ApplyAndSave(headRenderer, headMaterials[headIndex], "SavedHead", headIndex);
        
        if (bodyMaterials.Length > 0 && bodyRenderer != null) 
            ApplyAndSave(bodyRenderer, bodyMaterials[bodyIndex], "SavedBody", bodyIndex);
        
        if (feetMaterials.Length > 0 && feetRenderer != null) 
            ApplyAndSave(feetRenderer, feetMaterials[feetIndex], "SavedFeet", feetIndex);
    }
}