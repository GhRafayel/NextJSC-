export function getAvatarSrc(avatar: string | undefined): string {
    if (!avatar) return '/avatar/hero-gray.svg';
    if (avatar.startsWith('uploads/')) return `${process.env.NEXT_PUBLIC_API_URL}/${avatar}`;
    return `/avatar/${avatar}`;
}
