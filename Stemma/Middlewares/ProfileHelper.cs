using System.Management;

namespace Stemma.Middlewares
{
    // yeet
    public static class ProfileHelper
    {
        public static string GetProfileSvg(string base64Image, string userName)
        {
            string svgContent = "";

            svgContent = $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""300"" height=""285"" viewBox=""0 0 300 285"" fill=""none"" role=""img"" aria-labelledby=""descId"" x=""0"" y=""0"">
  <title id=""descId"">Circular Image</title>
  <defs>
    <clipPath id=""circleClip"">
      <circle cx=""150"" cy=""142.5"" r=""90"" />
    </clipPath>
    <linearGradient id=""gradientStroke"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
      <stop offset=""0%"" stop-color=""blue""/>
      <stop offset=""30%"" stop-color=""blueviolet""/>
      <stop offset=""70%"" stop-color=""blueviolet""/>
      <stop offset=""100%"" stop-color=""purple""/>
    </linearGradient>
    
    <symbol id=""starlight1"" viewBox=""0 0 360 345"">
      <rect width=""360"" height=""345"" fill=""none""/>
      <g id=""starlight"">
        <defs>
          <linearGradient id=""starlightGradient"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
            <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
            <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
            <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
          </linearGradient>
        </defs>
        <rect width=""50"" height=""1"" fill=""url(#starlightGradient)"">
          <animateMotion 
            dur=""1.5s"" 
            repeatCount=""indefinite"" 
            rotate=""auto"" 
            values=""300,0;100,100"" />
          <animate 
            attributeName=""opacity"" 
            from=""1"" 
            to=""0"" 
            dur=""1.5s"" 
            repeatCount=""indefinite"" />
        </rect>
      </g>
    </symbol>
	
	<symbol id=""starlight2"" viewBox=""0 0 360 345"">
      <rect width=""360"" height=""345"" fill=""none""/>
      <g id=""starlight"">
        <defs>
          <linearGradient id=""starlightGradientLong"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
            <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
            <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
            <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
          </linearGradient>
        </defs>
        <rect width=""50"" height=""1"" fill=""url(#starlightGradientLong)"">
          <animateMotion 
            dur=""2s"" 
            repeatCount=""indefinite"" 
            rotate=""auto"" 
            values=""300,0;-100,200"" />
          <animate 
            attributeName=""opacity"" 
            from=""1"" 
            to=""0"" 
            dur=""2s"" 
            repeatCount=""indefinite"" />
        </rect>
      </g>
    </symbol>
	
	<symbol id=""starlight3"" viewBox=""0 0 360 345"">
      <rect width=""360"" height=""345"" fill=""none""/>
      <g id=""starlight"">
        <defs>
          <linearGradient id=""starlightGradientLong"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
            <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
            <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
            <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
          </linearGradient>
        </defs>
        <rect width=""50"" height=""1"" fill=""url(#starlightGradientLong)"">
          <animateMotion 
            dur=""1.2s"" 
            repeatCount=""indefinite"" 
            rotate=""auto"" 
            values=""300,0;-100,200"" />
          <animate 
            attributeName=""opacity"" 
            from=""1"" 
            to=""0"" 
            dur=""1.2s"" 
            repeatCount=""indefinite"" />
        </rect>
      </g>
    </symbol>
	
	<symbol id=""starlight4"" viewBox=""0 0 360 345"">
      <rect width=""360"" height=""345"" fill=""none""/>
      <g id=""starlight"">
        <defs>
          <linearGradient id=""starlightGradientLong"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
            <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
            <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
            <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
          </linearGradient>
        </defs>
        <rect width=""50"" height=""1"" fill=""url(#starlightGradientLong)"">
          <animateMotion 
            dur=""2.6s"" 
            repeatCount=""indefinite"" 
            rotate=""auto"" 
            values=""300,0;-100,200"" />
          <animate 
            attributeName=""opacity"" 
            from=""1"" 
            to=""0"" 
            dur="".6s"" 
            repeatCount=""indefinite"" />
        </rect>
      </g>
    </symbol>
	
	
	<symbol id=""starlight6"" viewBox=""0 0 360 345"">
      <rect width=""360"" height=""345"" fill=""none""/>
      <g id=""starlight"">
        <defs>
          <linearGradient id=""starlightGradientLong"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"">
            <stop offset=""0%"" stop-color=""yellow"" stop-opacity=""0""/>
            <stop offset=""50%"" stop-color=""yellow"" stop-opacity=""1""/>
            <stop offset=""100%"" stop-color=""yellow"" stop-opacity=""1""/>
          </linearGradient>
        </defs>
        <rect width=""50"" height=""1"" fill=""url(#starlightGradientLong)"">
          <animateMotion 
            dur=""3.3s"" 
            repeatCount=""indefinite"" 
            rotate=""auto"" 
            values=""300,0;-100,200"" />
          <animate 
            attributeName=""opacity"" 
            from=""1"" 
            to=""0"" 
            dur=""3.3s"" 
            repeatCount=""indefinite"" />
        </rect>
      </g>
    </symbol>
  </defs>
  
  <rect data-testid=""card-bg"" x=""0.5"" y=""0.5"" rx=""4.5"" height=""99%"" width=""299"" fill=""#242424"" stroke=""#e4e2e2"" stroke-opacity=""1""/>
  
  <svg x=""-30"" y=""-30"" width=""360"" height=""345"" viewBox=""0 0 360 345"">
    <use href=""#starlight1"" x=""-100"" y=""30"" />
    <use href=""#starlight2"" x=""100"" y=""-20"" />
    <use href=""#starlight3"" x=""50"" y=""100"" />
    <use href=""#starlight4"" x=""-150"" y=""210"" />
    <use href=""#starlight2"" x=""-30"" y=""180"" />
    <use href=""#starlight6"" x=""80"" y=""210"" />
  </svg>
  
  <g transform=""translate(0, -20)"">
    <g clip-path=""url(#circleClip)"">
      <image 
        href=""data:image/jpeg;base64,{base64Image}""
        x=""50"" 
        y=""42.5"" 
        width=""200"" 
        height=""200"" 
        preserveAspectRatio=""xMidYMid slice""
      />
    </g>
    
    <circle cx=""150"" cy=""142.5"" r=""90"" stroke=""url(#gradientStroke)"" stroke-width=""10"" fill=""none"">
      <animateTransform attributeName=""transform"" type=""rotate"" from=""0 150 142.5"" to=""360 150 142.5"" dur=""5s"" repeatCount=""indefinite""/>
    </circle>
  </g>
  
  <text x=""150"" y=""260"" fill=""white"" font-family=""sans-serif"" font-size=""20"" font-weight=""bold"" text-anchor=""middle"">
    {userName}
  </text>
</svg>
";

            return svgContent;
        }

        public static async Task<IFormFile> GetAvatarAsFormFileAsync(string avatarUrl)
        {
            using (var httpClient = new HttpClient())
            {
                byte[] imageBytes = await httpClient.GetByteArrayAsync(avatarUrl);

                var stream = new MemoryStream(imageBytes);

                IFormFile formFile = new FormFile(stream, 0, stream.Length, "avatar", "avatar.jpg");

                return formFile;
            }
        }
    }
}
