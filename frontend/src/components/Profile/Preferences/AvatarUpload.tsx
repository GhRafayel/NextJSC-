'use client';
import { useRef, useState } from 'react';
import Image from 'next/image';
import { Plus, Loader } from 'lucide-react';
import { useAuth } from '@/src/components/Provider/UserProvider';
import { getAvatarSrc } from '@/src/lib/avatar';

export default function AvatarUpload() {
    const { cntUser, ChangingCallback } = useAuth();
    const inputRef = useRef<HTMLInputElement>(null);
    const [uploading, setUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        setUploading(true);
        setError(null);
        try {
            const formData = new FormData();
            formData.append('file', file);
            const res = await fetch('/api/avatar-upload', { method: 'POST', body: formData });
            if (res.ok) {
                await ChangingCallback(undefined, 'me');
            } else {
                const body = await res.json().catch(() => null);
                setError(body?.error || 'Upload failed');
            }
        } catch {
            setError('Upload failed');
        } finally {
            setUploading(false);
            if (inputRef.current) inputRef.current.value = '';
        }
    };

    return (
        <div className="pf-avatarUpload">
            <div className="pf-avatarUploadPreview">
                <Image src={getAvatarSrc(cntUser?.avatar)} alt={cntUser?.Username || 'Avatar'} width={64} height={64} className="pf-avatarUploadImg" />
            </div>
            <button
                type="button"
                aria-label="Upload avatar"
                className="pf-avatarUploadBtn"
                onClick={() => inputRef.current?.click()}
                disabled={uploading}
            >
                {uploading ? <Loader size={14} className="animate-spin" /> : <Plus size={14} />}
            </button>
            <input
                ref={inputRef}
                type="file"
                accept="image/png,image/jpeg,image/gif,image/webp"
                className="hidden"
                onChange={handleFileChange}
            />
            {error && <span className="pf-avatarUploadError">{error}</span>}
        </div>
    );
}
