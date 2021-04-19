# Preda-Music-Lights
A c# extension that requires a couple inputs to output colors that work with audio
Easy to use with Unity3D or any c# project

Example usage in Unity3D:
```c#
    public Preda.PredaMusicLights visualizer; 
    public Image Background;

    private void Start()
    {
        visualizer = new Preda.PredaMusicLights();
        
        visualizer.Init("Samples");
    }
    
    private void Update()
    {
        if (Time.time < DataGatherTime) return;

        System.Drawing.Color UpdatedVisuals = visualizer.Update(Time.time, "Sound Data array with the length of Samples");
        Background.color = SystemColorToColor(UpdatedVisuals);
    }
```
