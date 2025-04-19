import type { NextApiRequest, NextApiResponse } from "next";

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse
) {
  if (req.method === "POST") {
    const { prompt, imageBase64 } = req.body;

    // Validate input
    if (!imageBase64 || !imageBase64.startsWith("data:image/")) {
      return res
        .status(400)
        .json({ error: "Missing or invalid imageBase64" });
    }

    const [header, base64Data] = imageBase64.split(",");
    const mimeType = header.split(":")[1].split(";")[0]; // e.g., image/png

    if (!base64Data || base64Data.length < 10) {
        return res.status(400).json({ error: "Base64 image data is missing or invalid." });
      }
      

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

      const data = await apiResponse.json();

        console.log("Gemini raw response:", JSON.stringify(data, null, 2));


      const description =
        data?.candidates?.[0]?.content?.parts?.[0]?.text?.trim() ?? null;

      res.status(200).json({ description });
    } catch (error) {
      console.error("Gemini API error:", error);
      res.status(500).json({ error: "Error generating description." });
    }
  } else {
    res.status(405).end(); // Method Not Allowed
  }
}
