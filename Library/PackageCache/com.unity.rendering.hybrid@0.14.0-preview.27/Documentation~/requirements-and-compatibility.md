# Requirements and compatibility

This page contains information on system requirements and compatibility of the Hybrid Renderer package.

## Render pipeline compatibility

Hybrid Renderer V1 and Hybrid Renderer V2 have different compatibility between render pipelines. For more information about the differences between Hybrid Renderer V1 and Hybrid Renderer V2, see [Hybrid Renderer versions](hybrid-renderer-versions.md).

### Hybrid Renderer V1

The following table shows the compatibility of Hybrid Renderer V1 with different render pipelines.

| **Render pipeline**                        | **Compatibility**                           |
| ------------------------------------------ | ------------------------------------------- |
| **Built-in Render Pipeline**               | Unity 2019.1 and above                      |
| **High Definition Render Pipeline (HDRP)** | Unity 2019.1 and above                      |
| **Universal Render Pipeline (URP)**        | URP version 8.0.0 in Unity 2020.1 and above |

 

### Hybrid Renderer V2

The following table shows the compatibility of Hybrid Renderer V2 with different render pipelines.

| **Render pipeline**                        | **Compatibility**                                         |
| ------------------------------------------ | --------------------------------------------------------- |
| **Built-in Render Pipeline**               | Not supported                                             |
| **High Definition Render Pipeline (HDRP)** | HDRP version 9.0.0 and above, with Unity 2020.1 and above |
| **Universal Render Pipeline (URP)**        | URP version 9.0.0 and above, with Unity 2020.1 and above  |

 

## Unity Player system requirements

This section describes the Hybrid Renderer package’s target platform requirements. For platforms or use cases not covered in this section, general system requirements for the Unity Player apply.

For more information, see [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).

Currently, Hybrid Renderer does not support desktop OpenGL or GLES. For Android and Linux, you should use Vulkan. However, be aware that the Vulkan drivers on many older Android devices are in a bad shape and will never be upgraded. This limits the platform coverage Hybrid Renderer can currently offer on Android devices.

In 2020, the focus for Hybrid Renderer was on editor platforms (Windows and Mac) to unblock DOTS content production. To help with stability and keep the editor platforms fully functional, Unity added automated testing for both Windows and Mac.

Hybrid Renderer is not yet validated on mobile and console platforms. The main focus for 2021 is to improve the editor platforms, support the remaining URP and HDRP features, and continue to improve the stability, performance, test coverage, and documentation to make Hybrid Renderer production ready. For mobile and console platforms, the aim is to gradually improve test coverage.

If your Project targets mobile and console platforms and is aiming for release in 2021, do not yet adapt Hybrid Renderer. DOTS is in preview. Early adopters should be prepared to find limitations and issues that need custom workarounds. The main focus for Hybrid Renderer in 2021 is to unblock productions, but the emphasis is on the editor platforms so there is limited bandwidth to help with mobile and console platform related issues.

Hybrid Renderer is not yet tested or supported on [XR](https://docs.unity3d.com/Manual/XR.html) devices. XR support is intended in a later version.

Hybrid Renderer does not support ray-tracing (DXR). Ray-tracing support is intended in a later version.

## DOTS feature compatibility

Hybrid Renderer does not support multiple DOTS [Worlds](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/manual/world.html). Limited support for multiple Worlds is intended in a later version. The current plan is to add support for creating multiple rendering systems, one per renderable World, but then only have one World active for rendering at once.