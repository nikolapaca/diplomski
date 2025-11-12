import { FormGroup } from "@angular/forms";
import { CardList } from "./cardList.model";

export interface CardListView extends CardList{
    showAddCardForm?: boolean,
    addCardForm?: FormGroup,
    showUpdateListForm?: boolean,
    updateListForm: FormGroup
}