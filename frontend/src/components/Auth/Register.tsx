"use client"

import { useRouter } from "next/navigation";
import { useState } from "react"
import { Lib } from "@/src/lib/lib";
import { useAuth } from "@/src/components/Provider/UserProvider";
import FormInputs from "./FormInputs";

export default function Register() {

	const router = useRouter();
	const registrData = Lib.data;
    const [password, setPassword] = useState("");
	const {ChangingCallback, cntUser, LENUAGE} = useAuth();

return (

	<div  className={`${cntUser?.theme ?? true ?  "bg text-gray-900" : "bg-gray-200"} w-full h-screen flex items-center justify-center px-4`} >

		<div className="w-full max-w-md mx-auto p-6 glass rounded-2xl">

			<div className="w-full text-center my-3">
				<h2 className="text-4xl font-bold"> {LENUAGE.Auth.register.title}
				</h2>
			</div>
			<form onSubmit={ async (e) => { 
					e.preventDefault();
					const form = Object.fromEntries(new FormData(e.currentTarget));
					if (form.Password != form.ConfirmPassword)
                        return setPassword(LENUAGE.Auth.register.passwordMismatch);
					await Lib.postRequest("/api/auth?path=/auth/register", {...form})
					.then(res => res.ok ? (ChangingCallback(undefined, "me") , router.push("/")) : console.log(res));
				 }}
			>
				{registrData.map((item, i) => (<FormInputs key={i} item={item} placeholder={LENUAGE.Auth.fields[item.name as keyof typeof LENUAGE.Auth.fields]}/>))}
				
				<div className="m-5 text-center">
                    { password.length > 1 && ( <div className="text-red-700 m-2"> {password} </div> ) }
					<div>
						<button className="formBtnSubmit" type="submit" >
							{LENUAGE.Auth.register.submit}
						</button>
					</div>
				</div>
			</form>
		</div>
	</div>
)}
