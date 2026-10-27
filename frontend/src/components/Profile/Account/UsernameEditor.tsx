'use client';
import { useState } from 'react';
import { useAuth } from '@/src/components/Provider/UserProvider';

export default function UsernameEditor() {

  const { cntUser, LENUAGE, ChangingCallback } = useAuth();
  const profil = LENUAGE.Profile;
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(cntUser?.Username ?? '');
  const [error, setError] = useState(false);

  if (!editing) {
    return (
      <div className="pf-headerNameRow">
        <h2 className="pf-headerName flex-1 min-w-0">{cntUser?.Username || '—'}</h2>
        <button type="button" className="pf-btn pf-btnGhost shrink-0"
          onClick={() => { setValue(cntUser?.Username ?? ''); setError(false); setEditing(true); }}
        >
          {profil.settings.username.edit}
        </button>
      </div>
    );
  }

  return (
    <form className="pf-headerNameRow" onSubmit={async (e) => {
        e.preventDefault();
        const Username = value.trim();
        try {
          await ChangingCallback({ Username }, 'change-username');
          setEditing(false);
        } catch {
          setError(true);
        }
      }}
    >
      <input className="pf-input pf-headerNameInput" value={value} minLength={3} maxLength={40} required autoFocus
        onChange={(e) => setValue(e.target.value)}
      />
      <button type="submit" className="pf-btn pf-btnPrimary shrink-0">{profil.sub}</button>
      {error && <span className="pf-message">{profil.settings.message}</span>}
    </form>
  );
}
