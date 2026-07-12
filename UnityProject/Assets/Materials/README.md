# Materials

Intentionally empty for the prototype. Materials are created at runtime from
the Standard shader and colored per product data (`SceneObjectFactory.cs`).

Note: because no asset references the Standard shader, it must be added to
**Project Settings → Graphics → Always Included Shaders** for WebGL builds.
The build script (`Assets/Editor/WebGLBuilder.cs`) does this automatically.
