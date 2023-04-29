void getPixelDimensions_half (half2 uv, out half2 dimensions)
{
    dimensions = fwidth(uv);
}

void getPixelDimensions_float (float2 uv, out float2 dimensions)
{
    dimensions = fwidth(uv);
}
