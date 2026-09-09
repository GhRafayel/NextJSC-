import { useState }   from "react";
import { useRouter }  from "next/navigation";
import { Lib }        from "@/src/lib/lib";
import { useAuth }    from "@/src/components/Provider/UserProvider";


export default function ResetCodePage( {state} :  { state: string }) {
  const [code, setCode] = useState("");
  const [request, setRequest] = useState("");
  const [loading, setLoading] = useState(false);
  const	router = useRouter();
  const { cntUser, LENUAGE } = useAuth();

  return (
    <div className={`${cntUser?.theme ?? true ? "bg" : "bg-gray-200"} w-full h-screen flex items-center justify-center px-4`}>
        <div className="w-full max-w-md mx-auto p-6 glass rounded-2xl">

          <h1 className="text-2xl sm:text-3xl font-bold text-center">{LENUAGE.Auth.resetCode.title} </h1>

          <form className="mt-8 space-y-5" onSubmit={async(e) => {
              e.preventDefault();
              setLoading(true);
              const res = await Lib.postRequest("/api/edit?path=/auth/resetCode", {Email: state, code})
              if (res.ok) {
                router.push("/server/login");
              } else {
                setLoading(false);
                setRequest(LENUAGE.Auth.resetCode.error);
              }
          }} >
            <div>
              <label htmlFor="resetCode" className="block text-sm font-medium mb-2"> {LENUAGE.Auth.resetCode.label} </label>

              <input  id="resetCode" type="text" autoFocus value={code} placeholder={LENUAGE.Auth.resetCode.placeholder} maxLength={6} className="restCodeInput"
                      onChange={(e) => setCode(e.target.value)} />
            </div>

            <button type="submit" disabled={loading || code.length < 6} className=" formBtnSubmit " >
              {loading ? LENUAGE.Auth.resetCode.verifying : LENUAGE.Auth.resetCode.verify}
            </button>

          </form>

          <div className="text-center mt-6">
            <p className="text-sm text-red-900">{request} </p>
          </div>

        </div>
    </div>
  );
}