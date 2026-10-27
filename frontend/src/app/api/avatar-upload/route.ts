import { NextRequest, NextResponse } from "next/server";
import { getAccessTokenFromCookie } from "@/src/app/api/edit/route";

const serverUrl = process.env.INTERNAL_API_URL;

export async function POST(request: NextRequest) {
    const { accessToken } = await getAccessTokenFromCookie();
    if (!accessToken)
        return NextResponse.json({ error: "Unauthorized" }, { status: 401 });

    const formData = await request.formData();

    const res = await fetch(serverUrl + "/users/avatar-upload", {
        method: "POST",
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
        body: formData,
    });

    const result = await res.json();
    return NextResponse.json(result, { status: res.status });
}
