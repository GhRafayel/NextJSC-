import * as signalR from "@microsoft/signalr"

let connection : signalR.HubConnection | null =  null;


function isLocalWithoutNginx(): boolean {
  return window.location.port === "3000";
}

function localSocketUrl(): string {
  return `${window.location.protocol}//${window.location.hostname}:4000`;
}

function ensureConnection(): signalR.HubConnection | null {
  if (connection) return connection;
  if (typeof window === "undefined") return null;

  const url = process.env.NEXT_PUBLIC_SOCKET_URL || (isLocalWithoutNginx() ? localSocketUrl() : `${window.location.protocol}//${window.location.host}`);

    connection = new signalR.HubConnectionBuilder()
    .withUrl(`${url}/hubs/game`, {
      accessTokenFactory: async () => {
        const res = await fetch("/api/socket-token");
        const data = await res.json();
        
        return data.accessToken ?? "";
      },
    })
    .configureLogging(signalR.LogLevel.None)
    .build();
  return connection;
}

export function useSocket() {
    return ensureConnection();
}
