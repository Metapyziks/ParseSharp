shader Sprite
{
    options
    {
        Enable Blend;
        BlendFunc SrcAlpha OneMinusSrcAlpha;
    }

    in vec2 iPosition;
    in vec2 iTexture;
    in vec4 iColor;

    [VertOutPosition2D]
    varying vec2 vPosition;
    varying vec2 vTexture;
    varying vec4 vColor;

    uniform sampler2D uSprite;

    [FragOutColor]
    out vec4 oColor;

    [VertFunction]
    void vert()
    {
        vPosition = iPosition;
        vTexture = iTexture;
        vColor = iColor;
    }

    [FragFunction]
    void frag()
    {
        oColor = texture2D(uSprite, vTexture) * vColor;

        if (true)
        {
            doSomething();
        }
    }
}
