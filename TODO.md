Remove the half-texel offset in linear filtering.  Let the caller decide.
However, a texture atlas should pack with padding so as to eliminate this for the common case
(This will mean undoing the changes in SpriteBatchPMO)
Look at supporting Texture2DArray, so that we can have the terrain on one atlas and the text on another.
-Look at stencil buffers for the UI components to enforce clipping cheaply
