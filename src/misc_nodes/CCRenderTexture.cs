using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CocosSharp
{

    public enum CCRenderTargetUsage
    {
        DiscardContents,
        PreserveContents,
        PlatformContents
    }

    public partial class CCRenderTexture : CCNode
    {

        [Flags]
        enum ClearFlags
        {
            None           =           0x0,
            ColorBuffer    =           0x1,
            DepthBuffer    =           0x2,
            StencilBuffer  =           0x4,
            All            = DepthBuffer | StencilBuffer | ColorBuffer 
        }


        bool firstUsage = true;
        RenderTarget2D renderTarget2D;

        #region Properties

        public CCSprite Sprite { get; set; }
        public CCTexture2D Texture { get; private set; }
        protected CCSurfaceFormat PixelFormat { get; private set; }

        #endregion Properties

        CCColor4B beginClearColor = CCColor4B.Transparent;
        float beginDepthValue = 0;
        int beginStencilValue = 0;

        ClearFlags clearFlags = ClearFlags.None;

        #region Constructors

        public CCRenderTexture()
        {
            PixelFormat = CCSurfaceFormat.Color;
        }

        public CCRenderTexture(CCSize contentSize, CCSize textureSizeInPixels, 
            CCSurfaceFormat colorFormat=CCSurfaceFormat.Color, 
            CCDepthFormat depthFormat=CCDepthFormat.None, 
            CCRenderTargetUsage usage=CCRenderTargetUsage.DiscardContents)
        {
            int textureWidth = (int)textureSizeInPixels.Width;
            int textureHeight = (int)textureSizeInPixels.Height;

            firstUsage = true;
            renderTarget2D = CCDrawManager.SharedDrawManager.CreateRenderTarget(textureWidth, textureHeight, colorFormat, depthFormat, usage);

            Texture = new CCTexture2D(renderTarget2D, colorFormat, true, false);
            Texture.IsAntialiased = false;

            Sprite = new CCSprite(Texture);
            Sprite.ContentSize = contentSize;
            Sprite.BlendFunc = CCBlendFunc.AlphaBlend;

            // Make sure we set our content size or AnchorPoint will not work correctly
            ContentSize = textureSizeInPixels;

            AddChild(Sprite);
        }

        #endregion Constructors


        public virtual void Begin()
        {
            CCDrawManager drawManager = CCDrawManager.SharedDrawManager;

            // Save the current matrix
            drawManager.PushMatrix();

//            Matrix projection = Matrix.CreateOrthographicOffCenter(
//                -1.0f / widthRatio, 1.0f / widthRatio,
//                -1.0f / heightRatio, 1.0f / heightRatio,
//                -1, 1
//            );
//
//            drawManager.MultMatrix(ref projection);

            drawManager.SetRenderTarget(Texture);

            if (firstUsage)
            {
                drawManager.Clear(beginClearColor);
                firstUsage = false;
            }
        }

        public void BeginWithClear(float r, float g, float b, float a)
        {
            Begin();
            beginClearColor = new CCColor4B(r, g, b, a);
            clearFlags = ClearFlags.ColorBuffer;
            CCDrawManager.SharedDrawManager.Clear(beginClearColor);
        }

        public void BeginWithClear(float r, float g, float b, float a, float depthValue)
        {
            Begin();
            beginClearColor = new CCColor4B(r, g, b, a);
            beginDepthValue = depthValue;
            clearFlags = ClearFlags.ColorBuffer | ClearFlags.DepthBuffer;
            CCDrawManager.SharedDrawManager.Clear(beginClearColor, depthValue);
        }

		public void BeginWithClear(float r, float g, float b, float a, float depthValue, int stencilValue)
		{
			Begin();
            beginClearColor = new CCColor4B(r, g, b, a);
            beginDepthValue = depthValue;
            beginStencilValue = stencilValue;
            clearFlags = ClearFlags.All;
			CCDrawManager.SharedDrawManager.Clear(beginClearColor, depthValue, stencilValue);
		}

		public void BeginWithClear(CCColor4B col)
		{
			Begin();
            beginClearColor = col;
            clearFlags = ClearFlags.ColorBuffer;
			CCDrawManager.SharedDrawManager.Clear(col);
		}

		public void BeginWithClear(CCColor4B col, float depthValue)
		{
			Begin();
            beginClearColor = col;
            beginDepthValue = depthValue;
            clearFlags = ClearFlags.ColorBuffer | ClearFlags.DepthBuffer;

			CCDrawManager.SharedDrawManager.Clear(col, depthValue);
		}

		public void BeginWithClear(CCColor4B col, float depthValue, int stencilValue)
		{
			Begin();
            beginClearColor = col;
            beginDepthValue = depthValue;
            beginStencilValue = stencilValue;
            clearFlags = ClearFlags.All;
			CCDrawManager.SharedDrawManager.Clear(col, depthValue, stencilValue);
		}

        public void ClearDepth(float depthValue)
        {
            Begin();
            beginDepthValue = depthValue;
            clearFlags |= ClearFlags.DepthBuffer;
            CCDrawManager.SharedDrawManager.Clear(ClearOptions.DepthBuffer, Microsoft.Xna.Framework.Color.White, depthValue, 0);
            End();
        }

        public void ClearStencil(int stencilValue)
        {
            Begin();
            beginStencilValue = stencilValue;
            clearFlags |= ClearFlags.StencilBuffer;
            CCDrawManager.SharedDrawManager.Clear(ClearOptions.Stencil, Microsoft.Xna.Framework.Color.White, 1, stencilValue);
            End();
        }

        public void Clear(float r, float g, float b, float a)
        {
            Clear(new CCColor4B(r, g, b, a));
        }

        public void Clear(CCColor4B col)
        {
            Begin();
            beginClearColor = col;
            clearFlags |= ClearFlags.ColorBuffer;
            CCDrawManager.SharedDrawManager.Clear(beginClearColor);
            End();
        }

        void Clear(ClearFlags clearFlags, CCColor4B color, float depth, int stencil)
        {
            if (clearFlags.HasFlag(ClearFlags.ColorBuffer))
                CCDrawManager.SharedDrawManager.Clear(color);

            if (clearFlags.HasFlag(ClearFlags.DepthBuffer))
                CCDrawManager.SharedDrawManager.Clear(ClearOptions.DepthBuffer, Microsoft.Xna.Framework.Color.White, depth, 0);

            if (clearFlags.HasFlag(ClearFlags.StencilBuffer))
                CCDrawManager.SharedDrawManager.Clear(ClearOptions.Stencil, Microsoft.Xna.Framework.Color.White, 1, stencil);

        }

        public virtual void End()
        {
            CCDrawManager.SharedDrawManager.PopMatrix();

            CCDrawManager.SharedDrawManager.SetRenderTarget((CCTexture2D) null);
        }

        public override void Visit()
        {

            if (!Visible)
                return;

            Sprite.XnaLocalMatrix = XnaLocalMatrix;
            Sprite.Visit();
            Draw();

        }

        public bool AutoDraw = false;
        protected override void Draw()
        {
            if (AutoDraw)
            {
                Begin();

                //Begin will create a render group using new render target
                Clear(clearFlags, beginClearColor, beginDepthValue, beginStencilValue);

                //! make sure all children are drawn
                SortAllChildren();

                foreach(var child in Children)
                {
                    if (child != Sprite)
                        child.Visit();
                }

                //End will pop the current render group
                End();
            }

            //base.Draw();
        }

        public bool SaveToStream(Stream stream, CCImageFormat format)
        {
            if (format == CCImageFormat.Png)
            {
                Texture.SaveAsPng(stream, Texture.PixelsWide, Texture.PixelsHigh);
            }
            else if (format == CCImageFormat.Jpg)
            {
                Texture.SaveAsJpeg(stream, Texture.PixelsWide, Texture.PixelsHigh);
            }
            else
            {
                return false;
            }
            
            return true;
        }
    }
}