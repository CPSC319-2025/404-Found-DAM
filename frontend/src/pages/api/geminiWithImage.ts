import type { NextApiRequest, NextApiResponse } from "next";

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse
) {
  if (req.method === "POST") {
    const { prompt, imageBase64 } = req.body;

    // Add detailed logging
    console.log("Received imageBase64 data that starts with:", 
      imageBase64 ? imageBase64.substring(0, 50) + "..." : "undefined");
    
    // More comprehensive validation
    if (!imageBase64) {
      return res.status(400).json({ error: "Missing imageBase64 data" });
    }
    
    if (!imageBase64.startsWith("data:image/")) {
      return res.status(400).json({ 
        error: "Invalid imageBase64 format. Must start with 'data:image/'" 
      });
    }

    try {
      // Extract base64 data after the comma
      const base64Data = imageBase64.split(",")[1];
      
      if (!base64Data || base64Data.length < 10) {
        return res.status(400).json({ error: "Base64 image data is missing or invalid." });
      }
      
      // Extract MIME type from the header
      const mimeType = imageBase64.split(";")[0].split(":")[1];

      console.log("Extracted MIME type:", mimeType);
      console.log("Base64 data length:", base64Data.length);

      try {
        const apiResponse = await fetch(
          `https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent?key=${process.env.GEMINI_API_KEY}`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              contents: [
                {
                  parts: [
                    { text: prompt },
                    {
                      inlineData: {
                        mimeType,
                        data: base64Data,
                      },
                    },
                  ],
                },
              ],
            }),
          }
        );

        if (!apiResponse.ok) {
          const errorText = await apiResponse.text();
          console.error("Gemini API returned an error:", apiResponse.status, errorText);
          return res.status(apiResponse.status).json({ 
            error: `Gemini API error: ${apiResponse.status}`, 
            details: errorText 
          });
        }

        const data = await apiResponse.json();
        console.log("Gemini raw response:", JSON.stringify(data, null, 2));

        const description =
          data?.candidates?.[0]?.content?.parts?.[0]?.text?.trim() ?? null;

        res.status(200).json({ description });
      } catch (error) {
        console.error("Gemini API error:", error);
        res.status(500).json({ error: "Error generating description from Gemini API." });
      }
    } catch (error) {
      console.error("Error processing base64 data:", error);
      res.status(500).json({ error: "Error processing image data" });
    }
  } else {
    res.status(405).json({ error: "Method not allowed. Use POST." }); // Method Not Allowed
  }
}