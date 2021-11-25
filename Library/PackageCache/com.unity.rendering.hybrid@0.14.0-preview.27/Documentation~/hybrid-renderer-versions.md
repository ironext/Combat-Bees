# Hybrid Renderer versions

This package contains two versions of Hybrid Renderer:

- [Hybrid Renderer V1](hybrid-renderer-v1.md) is the prototype hybrid rendering technology introduced in the [Megacity](https://unity.com/megacity) project and released in Unity 2019.1. It is no longer in active development.
- [Hybrid Renderer V2](hybrid-renderer-v2.md) is a new experimental hybrid rendering technology introduced in Unity 2020.1. It provides better performance and an improved feature set. It is in active development.

Hybrid Renderer V1 is no longer in active development and the supported feature set for all render pipelines is limited. Hybrid Renderer V2 is early in development and does not yet support every feature in URP or HDRP. The goal of Hybrid Renderer V2 is to eventually support the full URP and HDRP feature set.

## Feature support

The tables in this section compare render pipeline feature support between Hybrid Renderer V1 and Hybrid Renderer V2.

### URP features

| **Feature**                     | **V1** | **V2**           |
| ------------------------------- | ------ | ---------------- |
| **Material property overrides** | Yes    | Yes              |
| **Built-in property overrides** | No     | Yes              |
| **Shader Graph**                 | Yes    | Yes              |
| **Lit shader**                  | No     | Yes              |
| **Unlit shader**                | No     | Yes              |
| **RenderLayer**                 | No     | Yes              |
| **TransformParams**             | No     | Yes              |
| **DisableRendering**            | No     | Yes              |
| **Sun light**                   | Yes    | Yes              |
| **Point + spot lights**         | No     | Planned for 2021 |
| **Ambient probe**               | No     | Yes              |
| **Light probes**                | No     | Yes              |
| **Reflection probes**           | No     | Planned for 2021 |
| **Lightmaps**                   | No     | Yes              |
| **Shader keywords**             | No     | Planned for 2021 |
| **LOD crossfade**               | No     | Planned for 2021 |
| **Viewport shader override**    | No     | Planned for 2021 |
| **Transparencies (sorted)**     | No     | Yes              |
| **Occlusion culling (dynamic)** | No     | Experimental     |
| **Skinning / mesh deform**      | No     | Experimental     |

### HDRP features

| **Feature**                     | **V1** | **V2**           |
| ------------------------------- | ------ | ---------------- |
| **Material property overrides** | Yes    | Yes              |
| **Built-in property overrides** | No     | Yes              |
| **Shader Graph**                 | Yes    | Yes              |
| **Lit shader**                  | No     | Yes              |
| **Unlit shader**                | No     | Yes              |
| **Decal shader**                | No     | Yes              |
| **LayeredLit shader**           | No     | Yes              |
| **RenderLayer**                 | No     | Yes              |
| **TransformParams**             | No     | Yes              |
| **DisableRendering**            | No     | Yes              |
| **Motion blur**                 | No     | Yes              |
| **Temporal AA**                 | No     | Yes              |
| **Sun light**                   | Yes    | Yes              |
| **Point + spot lights**         | Yes    | Yes              |
| **Ambient probe**               | Yes    | Yes              |
| **Light probes**                | Yes    | Yes              |
| **Reflection probes**           | Yes    | Yes              |
| **Lightmaps**                   | No     | Yes              |
| **Shader keywords**             | No     | Planned for 2021 |
| **LOD crossfade**               | No     | Planned for 2021 |
| **Viewport shader override**    | No     | Planned for 2021 |
| **Transparencies (sorted)**     | No     | Yes              |
| **Occlusion culling (dynamic)** | No     | Experimental     |
| **Skinning / mesh deform**      | No     | Experimental     |