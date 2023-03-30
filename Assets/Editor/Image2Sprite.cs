using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Sprite[] Image2Sprite(Texture2D source, Vector2Int tileSize)
//{
//    // get the source dimensions
//    var sourceWidth = source.width;
//    var sourceHeight = source.height;

//    // Check how often the given tileSize fits into the image
//    var tileAmountX = Mathf.FloorToInt(sourceWidth / (float)tileSize.x);
//    var tileAmountY = Mathf.FloorToInt(sourceHeight / (float)tileSize.y);

//    var output = new Sprite[tileAmountX * tileAmountY];

//    // Iterate through the tiles horizontal then vertical
//    // starting at the top left tile
//    for (var y = tileAmountY - 1; y >= 0; y--)
//    {
//        for (var x = 0; x < tileAmountX; x++)
//        {
//            // get the bottom left pixel coordinate of the current tile
//            var bottomLeftPixelX = x * tileSize.x;
//            var bottomLeftPixelY = y * tileSize.y;

//            // instead of having to get the pixel data
//            // For a sprite you just have to set the rect from which part of the texture to create a sprite
//            var sprite = Sprite.Create(source, new Rect(bottomLeftPixelX, bottomLeftPixelY, tileSize.x, tileSize.y), Vector2.one * 0.5f);

//            output[x + y * tileAmountX] = sprite;
//        }
//    }

//    return output;
//}
